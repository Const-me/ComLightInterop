using ComLight.Emit;
using ComLight.Marshalling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ComLight
{
	/// <summary>Wraps managed interfaces into COM objects callable by native code.</summary>
	public static class ManagedWrapper
	{
		/// <summary>When native code doesn't bother calling AddRef on these interfaces, the lifetime of the wrappers is linked to the lifetime of the interface objects. This class implements that link.</summary>
		static class WrappersCache<I> where I : class
		{
			static readonly ConditionalWeakTable<I, ManagedObject> table = new ConditionalWeakTable<I, ManagedObject>();

			public static void add( I obj, ManagedObject wrapper )
			{
				table.Add( obj, wrapper );
			}

			public static IntPtr? lookup( I obj )
			{
				ManagedObject result;
				if( !table.TryGetValue( obj, out result ) )
					return null;
				return result.address;
			}
		}

		/// <summary>Expression visitor which replaces a specific ParameterExpression with a ConstantExpression</summary>
		class ParamReplacementVisitor: ExpressionVisitor
		{
			readonly ParameterExpression replacing;
			readonly ConstantExpression replacement;

			public ParamReplacementVisitor( ParameterExpression replacing, ConstantExpression replacement )
			{
				this.replacing = replacing;
				this.replacement = replacement;
			}

			protected override Expression VisitParameter( ParameterExpression node )
			{
				if( node == replacing )
					return replacement;
				else
					return base.VisitParameter( node );
			}
		}

		/// <summary>Expression tree prefab for a single interface method.</summary>
		class MethodBuilder
		{
			/// <summary>The first one is always ManagedWrapper.paramNativeObject, the rest of them vary depending on what was in the interface.</summary>
			readonly ParameterExpression[] nativeParameters;
			/// <summary>It has one extra parameter, "managed", replaced when compiling.</summary>
			readonly BlockExpression expression;
			/// <summary>Native function delegate it builds</summary>
			readonly Type tDelegate;

			/// <summary>Build the prefab</summary>
			public MethodBuilder( ParameterExpression managed, MethodInfo mi, Type tDelegate )
			{
				this.tDelegate = tDelegate;

				ParameterInfo[] parameters = mi.GetParameters();
				nativeParameters = new ParameterExpression[ parameters.Length + 1 ];
				nativeParameters[ 0 ] = paramNativeObject;

				Expression[] managedParameters = new Expression[ parameters.Length ];
				List<ParameterExpression> localVars = new List<ParameterExpression>();
				List<Expression> block = new List<Expression>();
				for( int i = 0; i < parameters.Length; i++ )
				{
					var pi = parameters[ i ];
					Type tp = pi.ParameterType;

					var cm = pi.customMarshaller();
					if( null != cm )
						tp = cm.getNativeType( pi );
					ParameterExpression pNative = Expression.Parameter( tp, pi.Name );
					Expressions custom = cm?.managed( pNative, !pi.IsOut );

					nativeParameters[ i + 1 ] = pNative;
					if( null != custom )
					{
						managedParameters[ i ] = custom.argument;
						if( null != custom.variable )
							localVars.Add( custom.variable );
						if( null != custom.after )
							block.Add( custom.after );
					}
					else
						managedParameters[ i ] = pNative;
				}

				if( mi.ReturnType != typeof( IntPtr ) )
				{
					Expression eCall = Expression.Call( managed, mi, managedParameters );
					if( mi.ReturnType == typeof( int ) )
						eCall = Expression.Return( returnTarget, eCall );
					else if( mi.ReturnType == typeof( bool ) )
						eCall = Expression.Return( returnTarget, Expression.Condition( eCall, Expression.Constant( IUnknown.S_OK ), Expression.Constant( IUnknown.S_FALSE ) ) );
					else
						Debug.Assert( mi.ReturnType == typeof( void ) );

					Expression eTryCatch = Expression.TryCatch( eCall, exprCatchBlock );
					Expression eReturnLabel = Expression.Label( returnTarget, Expression.Constant( IUnknown.S_OK, typeof( int ) ) );

					block.Insert( 0, eTryCatch );
					block.Add( eReturnLabel );

					expression = Expression.Block( typeof( int ), localVars, block );
				}
				else
				{
					// Pointer-returning methods require some special handling here, e.g. we can't reuse some pieces cached in these static readonly fields.
					LabelTarget ptrReturnTarget = Expression.Label( typeof( IntPtr ) );
					var eException = Expression.Parameter( typeof( Exception ), "ex" );

					// When C# method that returns IntPtr throws an exception, we ignore the exception and silently return nullptr.
					// Not sure what else we can do, ComLight doesn't include any logging infrastructure, and C++ can't catch .NET exceptions.
					var eCatchBody = Expression.Return( ptrReturnTarget, Expression.Constant( IntPtr.Zero ) );
					var catchAndReturnNull = Expression.Catch( eException, eCatchBody );

					Expression eCall = Expression.Call( managed, mi, managedParameters );
					eCall = Expression.Return( ptrReturnTarget, eCall );

					Expression eTryCatch = Expression.TryCatch( eCall, catchAndReturnNull );
					Expression eReturnLabel = Expression.Label( ptrReturnTarget, Expression.Constant( IntPtr.Zero ) );

					block.Insert( 0, eTryCatch );
					block.Add( eReturnLabel );

					expression = Expression.Block( typeof( IntPtr ), localVars, block );
				}
			}

			/// <summary>Apply the expression tree visitor finalizing the prefab, and compile into lambda.</summary>
			public Delegate compile( ExpressionVisitor visitor )
			{
				Expression eBody = visitor.Visit( expression );
				LambdaExpression lambda = Expression.Lambda( tDelegate, eBody, nativeParameters );
				return lambda.Compile();
			}
		}

		/// <summary>`IntPtr pNative` argument</summary>
		static readonly ParameterExpression paramNativeObject;
		/// <summary>Return label with int type</summary>
		static readonly LabelTarget returnTarget;
		/// <summary>Catch block that returns <see cref="Exception.HResult" /> and jumps to <see cref="returnTarget" />.</summary>
		static readonly CatchBlock exprCatchBlock;

		static ManagedWrapper()
		{
			// Create sub-expressions which don't depend on the interface type. This saves a bit of resources.
			paramNativeObject = Expression.Parameter( typeof( IntPtr ), "pNative" );

			Type tException = typeof( Exception );
			MethodInfo miExceptionHresult = tException.GetProperty( "HResult" ).GetGetMethod();

			// Create return target and catch block, they don't depend on the interface nor the input object
			returnTarget = Expression.Label( typeof( int ) );
			var eException = Expression.Parameter( typeof( Exception ), "ex" );
			var eCatchBody = Expression.Return( returnTarget, Expression.Property( eException, miExceptionHresult ) );
			exprCatchBlock = Expression.Catch( eException, eCatchBody );
		}

		/// <summary>Expression tree prefabs for the complete COM interface</summary>
		class InterfaceBuilder
		{
			readonly Type tInterface;
			readonly ParameterExpression paramManagedObject;
			readonly MethodBuilder[] builders;

			/// <summary>Use reflection to build the prefab</summary>
			public InterfaceBuilder( Type tInterface )
			{
				this.tInterface = tInterface;

				paramManagedObject = Expression.Parameter( tInterface, "managed" );

				MethodInfo[] methods = tInterface.GetMethods();
				Type[] tDelegates = NativeDelegates.buildDelegates( tInterface );
				Debug.Assert( methods.Length == tDelegates.Length );

				builders = new MethodBuilder[ methods.Length ];
				for( int i = 0; i < methods.Length; i++ )
					builders[ i ] = new MethodBuilder( paramManagedObject, methods[ i ], tDelegates[ i ] );
			}

			/// <summary>Compile prefab into array of delegates. Delegate are of different types, each type has [UnmanagedFunctionPointer], and is compatible with the corresponding C++ interface method.</summary>
			public Delegate[] compile( object obj )
			{
				var managed = Expression.Constant( obj, tInterface );
				var visitor = new ParamReplacementVisitor( paramManagedObject, managed );

				Delegate[] results = new Delegate[ builders.Length ];
				for( int i = 0; i < results.Length; i++ )
					results[ i ] = builders[ i ].compile( visitor );
				return results;
			}
		}

		static readonly object syncRoot = new object();
		static readonly Dictionary<Type, Func<object, bool, IntPtr>> cache = new Dictionary<Type, Func<object, bool, IntPtr>>();

		static Func<object, bool, IntPtr> getFactory<I>() where I : class
		{
			Type tInterface = typeof( I );
			Func<object, bool, IntPtr> result;
			lock( syncRoot )
			{
				if( cache.TryGetValue( tInterface, out result ) )
					return result;

				Guid iid = ReflectionUtils.checkInterface( tInterface );
				// The builder gets captured by the lambda.
				// This is what we want, the constructor takes noticeable time, the code outside lambda runs once per interface type, the code inside lambda runs once per object instance.
				InterfaceBuilder builder = new InterfaceBuilder( tInterface );

				result = ( object obj, bool addRef ) =>
				{
					if( null == obj )
					{
						// Marshalling null
						return IntPtr.Zero;
					}
					Debug.Assert( obj is I );

					if( obj is RuntimeClass rc )
					{
						// That .NET object is not actually managed, it's a wrapper around C++ implemented COM interface.
						if( rc.iid == iid )
						{
							// It wraps around the same interface
							if( addRef )
								rc.addRef();
							return rc.nativePointer;
						}

						// It wraps around different interface. Call QueryInterface on the native object.
						return rc.queryInterface( iid, addRef );
					}

					// It could be the same managed object is reused across native calls. If that's the case, the cache already contains the native pointer.
					I managed = (I)obj;
					IntPtr? wrapped = WrappersCache<I>.lookup( managed );
					if( wrapped.HasValue )
						return wrapped.Value;

					Delegate[] delegates = builder.compile( managed );
					ManagedObject wrapper = new ManagedObject( managed, iid, delegates );
					WrappersCache<I>.add( managed, wrapper );
					if( addRef )
						wrapper.callAddRef();
					return wrapper.address;
				};

				cache.Add( tInterface, result );
				return result;
			}
		}

		/// <summary>Wrap a C# interface into a COM object callable from native code</summary>
		/// <typeparam name="I">COM interface type</typeparam>
		/// <param name="obj">COM interface instance</param>
		/// <param name="addRef">Pass True to increment native ref.counter, do that when you want to move ownership to C++ code.</param>
		/// <returns>Native COM pointer of the wrapper</returns>
		public static IntPtr wrap<I>( object obj, bool addRef ) where I : class
		{
			Func<object, bool, IntPtr> factory = getFactory<I>();
			return factory( obj, addRef );
		}
	}
}