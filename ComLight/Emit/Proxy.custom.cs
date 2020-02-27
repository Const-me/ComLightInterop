using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace ComLight.Emit
{
	static partial class Proxy
	{
		/// <summary>Expression tree prefab for a single interface method, which uses custom marshaling.
		/// The generated code calls managed delegate, compiled during late binding. That one calls native delegate.</summary>
		/// <remarks>
		/// <para>IL generator is too low level for custom marshallers, it’s trivially easy to emit code that crashes in runtime, and hard to debug what’s wrong with it.
		/// That’s why custom marshallers rely on user-provided Linq.Expressions.</para>
		/// <para>Now, in .NET Core, there’s no LambdaExpression.CompileToMethod. The only way to compile expressions, compile them into delegates.</para>
		/// </remarks>
		class CustomMarshallerMethod: iMethodPrefab
		{
			readonly ParameterExpression eNativeDelegate;
			readonly ParameterExpression[] managedParameters;
			/// <summary>It has two extra parameter replaced when compiling, paramNativeObject, and eNativeDelegate.</summary>
			readonly Expression methodExpression;
			/// <summary>Delegate type it builds, same prototype as the C# interface method</summary>
			public Type tManagedDelegate { get; private set; }
			readonly Type tNativeDelegate;
			readonly MethodInfo method;
			readonly int methodIndex;

			/// <summary>Build the prefab, it contains 2 expressions that need late binding, `tNativeDelegate nativeDelegate`, and `IntPtr nativeComPointer`, represented by paramNativeObject constant object.</summary>
			public CustomMarshallerMethod( MethodInfo mi, Type tNativeDelegate, int idx )
			{
				method = mi;
				methodIndex = idx;
				this.tNativeDelegate = tNativeDelegate;
				eNativeDelegate = Expression.Parameter( tNativeDelegate, "nativeDelegate" );
				ParameterInfo[] parameters = mi.GetParameters();
				managedParameters = parameters.Select( pi => Expression.Parameter( pi.ParameterType, pi.Name ) ).ToArray();
				// tDelegate = Expression.GetDelegateType( parameters.Select( pi => pi.ParameterType ).Concat( new Type[ 1 ] { mi.ReturnType } ).ToArray() );
				// Expression.GetDelegateType doesn't work on desktop .NET, crashes later saying "Unable to make a reference to a transient module from a non-transient module."
				// No big deal, creating them manually with emitManagedDelegate so they're in the same assembly.

				Expression[] nativeParameters = new Expression[ parameters.Length + 1 ];
				nativeParameters[ 0 ] = paramNativeObject;
				List<ParameterExpression> localVars = new List<ParameterExpression>();
				List<Expression> after = new List<Expression>();

				for( int i = 0; i < parameters.Length; i++ )
				{
					var pi = parameters[ i ];
					var cm = pi.customMarshaller();
					if( null == cm )
					{
						nativeParameters[ i + 1 ] = managedParameters[ i ];
						continue;
					}
					var customExpressions = cm.native( managedParameters[ i ], !pi.IsOut );
					nativeParameters[ i + 1 ] = customExpressions.argument;
					if( null != customExpressions.variable )
						localVars.Add( customExpressions.variable );
					if( null != customExpressions.after )
						after.Add( customExpressions.after );
				}

				List<Expression> block = new List<Expression>();
				MethodInfo miInvokeNative = eNativeDelegate.Type.GetMethod( "Invoke" );
				Expression eCall = Expression.Call( eNativeDelegate, miInvokeNative, nativeParameters );
				List<Expression> blockEntries = new List<Expression>();
				if( mi.ReturnType == typeof( void ) )
				{
					block.Add( Expression.Call( miThrow, eCall ) );
					block.AddRange( after );
				}
				else if( mi.ReturnType == typeof( int ) )
				{
					if( localVars.Count <= 0 && after.Count <= 0 )
					{
						methodExpression = eCall;
						return;
					}

					ParameterExpression hr = Expression.Variable( typeof( int ), "hr" );
					localVars.Add( hr );
					block.Add( Expression.Assign( hr, eCall ) );
					block.AddRange( after );
					LabelTarget returnTarget = Expression.Label( typeof( int ) );
					block.Add( Expression.Return( returnTarget, hr ) );
					block.Add( Expression.Label( returnTarget, Expression.Constant( IUnknown.E_UNEXPECTED ) ) );
				}
				else if( mi.ReturnType == typeof( bool ) )
				{
					ParameterExpression hr = Expression.Variable( typeof( int ), "hr" );
					localVars.Add( hr );
					block.Add( Expression.Assign( hr, eCall ) );
					block.AddRange( after );
					LabelTarget returnTarget = Expression.Label( typeof( bool ) );
					block.Add( Expression.Return( returnTarget, Expression.Call( miThrowRetBool, hr ) ) );
					block.Add( Expression.Label( returnTarget, MiscUtils.eFalse ) );
				}
				else
					throw new ArgumentException( $"Unsupported return type { mi.ReturnType.FullName }, must be int, void or bool" );

				methodExpression = Expression.Block( localVars, block );
			}

			Type iMethodPrefab.tCtorArg => tManagedDelegate;

			FieldBuilder iMethodPrefab.emitField( TypeBuilder tb )
			{
				return tb.DefineField( "cm_" + method.Name, tManagedDelegate, privateReadonly );
			}

			void iMethodPrefab.emitConstructorBody( ILGenerator il, int methodIndex, ref int ctorArgIndex, FieldBuilder field )
			{
				il.Emit( OpCodes.Ldarg_0 );
				il.loadArg( ctorArgIndex );
				ctorArgIndex++;
				il.Emit( OpCodes.Stfld, field );
			}

			void iMethodPrefab.emitMethod( MethodBuilder mb, FieldBuilder field )
			{
				ILGenerator il = mb.GetILGenerator();
				// Load the delegate from this
				il.Emit( OpCodes.Ldarg_0 );
				il.Emit( OpCodes.Ldfld, field );

				// Load arguments
				for( int i = 0; i < method.GetParameters().Length; i++ )
					il.loadArg( i + 1 );

				// Call the delegate
				MethodInfo invoke = field.FieldType.GetMethod( "Invoke" );
				il.Emit( OpCodes.Callvirt, invoke );
				il.Emit( OpCodes.Ret );
			}

			public void emitManagedDelegate( TypeBuilder tbDelegates )
			{
				// Create the delegate type
				TypeAttributes ta = TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.NestedPublic;
				TypeBuilder tb = tbDelegates.DefineNestedType( method.Name, ta, typeof( MulticastDelegate ) );

				// Create constructor for the delegate
				MethodAttributes ma = MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public;
				ConstructorBuilder cb = tb.DefineConstructor( ma, CallingConventions.Standard, new Type[] { typeof( object ), typeof( IntPtr ) } );
				cb.SetImplementationFlags( MethodImplAttributes.Runtime | MethodImplAttributes.Managed );
				cb.DefineParameter( 1, ParameterAttributes.In, "object" );
				cb.DefineParameter( 2, ParameterAttributes.In, "method" );

				// Create Invoke method for the delegate
				Type[] paramTypes = method.GetParameters().Select( pi => pi.ParameterType ).ToArray();
				var mb = tb.DefineMethod( "Invoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, method.ReturnType, paramTypes );
				mb.SetImplementationFlags( MethodImplAttributes.Runtime | MethodImplAttributes.Managed );

				tManagedDelegate = tb.CreateType();
			}

			/// <summary>Finalize the prefab and compile into lambda.</summary>
			public Delegate lateBind( LateBindVisitor visitor )
			{
				visitor.bindMethod( eNativeDelegate, methodIndex, tNativeDelegate );
				Expression eBody = visitor.Visit( methodExpression );
				LambdaExpression lambda = Expression.Lambda( tManagedDelegate, eBody, managedParameters );
				return lambda.Compile();
			}
		}

		static void buildManagedDelegates( Type tInterface, IEnumerable<CustomMarshallerMethod> enumMethods )
		{
			CustomMarshallerMethod[] methods = enumMethods.ToArray();
			if( methods.Length <= 0 )
				return;
			TypeBuilder tbDelegates = Assembly.moduleBuilder.emitStaticClass( tInterface.FullName + "_custom" );
			foreach( var m in methods )
				m.emitManagedDelegate( tbDelegates );
			tbDelegates.CreateType();
		}

		/// <summary>Replaces 2 late bound parameters in custom marshaled method with constant expressions.</summary>
		class LateBindVisitor: ExpressionVisitor
		{
			readonly ConstantExpression nativeObject;
			readonly IntPtr[] vtable;
			ParameterExpression replacing;
			Expression replacement;

			public LateBindVisitor( IntPtr nativeObject, IntPtr[] vtable )
			{
				this.nativeObject = Expression.Constant( nativeObject );
				this.vtable = vtable;
			}

			public void bindMethod( ParameterExpression peNative, int methodIndex, Type tNativeDelegate )
			{
				IntPtr nativeFunction = vtable[ methodIndex + 3 ];
				Delegate nativeDelegate = Marshal.GetDelegateForFunctionPointer( nativeFunction, tNativeDelegate );
				ConstantExpression ceNative = Expression.Constant( nativeDelegate, tNativeDelegate );

				replacing = peNative;
				replacement = ceNative;
			}

			protected override Expression VisitParameter( ParameterExpression node )
			{
				if( node == paramNativeObject )
					return nativeObject;
				if( node == replacing )
					return replacement;
				return base.VisitParameter( node );
			}
		}

		/// <summary>Create a function which takes native pointer + vtable, and compiles custom marshaled methods for a COM interface.</summary>
		static Func<IntPtr, IntPtr[], Delegate[]> createLateBinder( CustomMarshallerMethod[] customMethods )
		{
			return ( IntPtr nativeObject, IntPtr[] vtable ) =>
			{
				LateBindVisitor visitor = new LateBindVisitor( nativeObject, vtable );
				Delegate[] delegates = new Delegate[ customMethods.Length ];
				for( int i = 0; i < customMethods.Length; i++ )
					delegates[ i ] = customMethods[ i ].lateBind( visitor );
				return delegates;
			};
		}
	}
}