using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ComLight
{
	/// <summary>Wraps managed interfaces into COM objects callable by native code.</summary>
	static class ManagedWrapper
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

		class MethodBuilder
		{
			/// <summary>The first one is always InterfaceBuilder.paramNativeObject, the rest of them vary depending on what was in the interface.</summary>
			readonly ParameterExpression[] nativeParameters;
			/// <summary>It has one extra parameter, "managed", bound when building.</summary>
			readonly BlockExpression expression;

			public MethodBuilder( ParameterExpression native, ParameterExpression managed, MethodInfo mi )
			{
				ParameterInfo[] parameters = mi.GetParameters();
				nativeParameters = new ParameterExpression[ parameters.Length + 1 ];
				nativeParameters[ 0 ] = native;
				for( int i = 0; i < parameters.Length; i++ )
				{
					var pi = parameters[ i ];
					nativeParameters[ i + 1 ] = Expression.Parameter( pi.ParameterType, pi.Name );
				}

				Expression eCall = Expression.Call( managed, mi, nativeParameters.Skip( 1 ) );
				if( mi.ReturnType == typeof( int ) )
					eCall = Expression.Return( returnTarget, eCall );
				Expression eTryCatch = Expression.TryCatch( eCall, exprCatchBlock );
				Expression eReturnLabel = Expression.Label( returnTarget, Expression.Constant( IUnknown.S_OK, typeof( int ) ) );
				expression = Expression.Block( typeof( int ), eTryCatch, eReturnLabel );
			}

			public Delegate build( ParamReplacementVisitor visitor, Type tDelegate )
			{
				Expression eBody = visitor.Visit( expression );
				LambdaExpression lambda = Expression.Lambda( tDelegate, eBody, nativeParameters );
				return lambda.Compile();
			}
		}

		static readonly LabelTarget returnTarget;
		static readonly CatchBlock exprCatchBlock;

		static ManagedWrapper()
		{
			Type tException = typeof( Exception );
			MethodInfo miExceptionHresult = tException.GetProperty( "HResult" ).GetGetMethod();

			// Create return target and catch block, they don't depend on the interface nor the input object
			returnTarget = Expression.Label( typeof( int ) );
			var eException = Expression.Parameter( typeof( Exception ), "ex" );
			var eCatchBody = Expression.Return( returnTarget, Expression.Property( eException, miExceptionHresult ) );
			exprCatchBlock = Expression.Catch( eException, eCatchBody );
		}

		class InterfaceBuilder<I>
			where I : class
		{
			readonly ParameterExpression paramNativeObject;
			readonly ParameterExpression paramManagedObject;
			readonly MethodBuilder[] builders;

			public InterfaceBuilder()
			{
				paramNativeObject = Expression.Parameter( typeof( IntPtr ), "pNative" );
				paramManagedObject = Expression.Parameter( typeof( I ), "managed" );
				builders = typeof( I ).GetMethods().Select( mi => new MethodBuilder( paramNativeObject, paramManagedObject, mi ) ).ToArray();
			}

			public Delegate[] build( Type[] tDelegates, I obj )
			{
				var managed = Expression.Constant( obj, typeof( I ) );
				var visitor = new ParamReplacementVisitor( paramManagedObject, managed );

				Delegate[] results = new Delegate[ builders.Length ];
				for( int i = 0; i < results.Length; i++ )
					results[ i ] = builders[ i ].build( visitor, tDelegates[ i ] );
				return results;
			}
		}

		static readonly object syncRoot = new object();
		static readonly Dictionary<Type, Func<object, IntPtr>> cache = new Dictionary<Type, Func<object, IntPtr>>();

		static Func<object, IntPtr> getFactory<I>() where I : class
		{
			Func<object, IntPtr> result;
			lock( syncRoot )
			{
				if( cache.TryGetValue( typeof( I ), out result ) )
					return result;

				Guid iid = ReflectionUtils.checkInterface( typeof( I ) );
				Type[] delegateTypes = WrapInterface.buildDelegates( typeof( I ) );
				var builder = new InterfaceBuilder<I>();

				result = ( object obj ) =>
				{
					if( null == obj )
					{
						// Marshalling null
						return IntPtr.Zero;
					}

					RuntimeClass rc = obj as RuntimeClass;
					if( null != rc )
					{
						// That .NET object is not actually managed, it's a wrapper around C++ implemented COM interface.
						if( rc.iid == iid )
						{
							// It wraps around the same interface
							return rc.nativePointer;
						}

						// It wraps around different interface. Call QueryInterface on the native object.
						return rc.queryInterface( iid );
					}

					// It could be the same managed object is reused across native calls. If that's the case, the cache already contains the native pointer.
					I managed = (I)obj;
					IntPtr? wrapped = WrappersCache<I>.lookup( managed );
					if( wrapped.HasValue )
						return wrapped.Value;

					Delegate[] delegates = builder.build( delegateTypes, managed );
					ManagedObject wrapper = new ManagedObject( managed, iid, delegates );
					WrappersCache<I>.add( managed, wrapper );
					return wrapper.address;
				};

				cache.Add( typeof( I ), result );
				return result;
			}
		}

		public static IntPtr wrap<I>( object obj ) where I : class
		{
			Func<object, IntPtr> factory = getFactory<I>();
			return factory( obj );
		}
	}
}