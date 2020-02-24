using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace ComLight.Emit
{
	static partial class Proxy
	{
		/// <summary>A method without custom marshaling. The generated code calls native delegate directly.
		/// The native delegate field is initialized in the constructor of the proxy.</summary>
		class ProxyMethod: iMethodPrefab
		{
			readonly MethodInfo method;
			readonly Type tNativeDelegate;

			public ProxyMethod( MethodInfo mi, Type tNativeDelegate )
			{
				method = mi;
				this.tNativeDelegate = tNativeDelegate;
			}

			FieldBuilder iMethodPrefab.emitField( TypeBuilder tb )
			{
				return tb.DefineField( "m_" + method.Name, tNativeDelegate, privateReadonly );
			}

			// This version builds native delegate in constructor, doesn't need extra arguments.
			Type iMethodPrefab.tCtorArg => null;

			void iMethodPrefab.emitConstructorBody( ILGenerator il, int methodIndex, ref int ctorArgIndex, FieldBuilder field )
			{
				il.Emit( OpCodes.Ldarg_0 );
				il.Emit( OpCodes.Ldarg_2 );
				il.pushIntConstant( methodIndex + 3 );
				il.Emit( OpCodes.Ldelem_I );
				// Specialize the generic Marshal.GetDelegateForFunctionPointer method with the delegate type
				MethodInfo mi = miGetDelegate.MakeGenericMethod( tNativeDelegate );
				il.Emit( OpCodes.Call, mi );
				// Store delegate in the readonly field
				il.Emit( OpCodes.Stfld, field );
			}

			void iMethodPrefab.emitMethod( MethodBuilder mb, FieldBuilder field )
			{
				ParameterInfo[] parameters = method.GetParameters();

				// Method body
				ILGenerator il = mb.GetILGenerator();
				// Load the delegate from this
				il.Emit( OpCodes.Ldarg_0 );
				il.Emit( OpCodes.Ldfld, field );
				// Load nativePointer from the base class
				il.Emit( OpCodes.Ldarg_0 );
				il.Emit( OpCodes.Ldfld, fiNativePointer );
				// Load arguments
				for( int i = 0; i < parameters.Length; i++ )
					il.loadArg( i + 1 );
				// Call the delegate
				MethodInfo invoke = field.FieldType.GetMethod( "Invoke" );
				il.Emit( OpCodes.Callvirt, invoke );

				if( method.ReturnType == typeof( void ) )
				{
					// Call ErrorCodes.throwForHR
					il.EmitCall( OpCodes.Call, miThrow, null );
				}
				else if( method.ReturnType == typeof( bool ) )
					il.EmitCall( OpCodes.Call, miThrowRetBool, null );
				else
					Debug.Assert( method.ReturnType == typeof( int ) );

				il.Emit( OpCodes.Ret );
			}
		}
	}
}