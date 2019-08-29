using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace ComLight.Emit
{
	/// <summary>Build static class with the native function pointer delegates</summary>
	static class NativeDelegates
	{
		/// <summary>Constructor of UnmanagedFunctionPointerAttribute</summary>
		static readonly ConstructorInfo ciFPAttribute;

		static NativeDelegates()
		{
			Type tPointerAttr = typeof( UnmanagedFunctionPointerAttribute );
			ciFPAttribute = tPointerAttr.GetConstructor( new Type[ 1 ] { typeof( CallingConvention ) } );
		}

		/// <summary>Create static class with the delegates</summary>
		static TypeBuilder createDelegatesType( Type tInterface )
		{
			return Assembly.moduleBuilder.emitStaticClass( tInterface.Name + "_native" );
		}

		static void defineDelegateParameters( MethodBuilder mb, ParameterInfo[] methodParams )
		{
			mb.DefineParameter( 1, ParameterAttributes.In, "pThis" );
			for( int i = 0; i < methodParams.Length; i++ )
			{
				ParameterInfo pi = methodParams[ i ];
				ParameterBuilder pb = mb.DefineParameter( i + 2, pi.Attributes, pi.Name );
				ParamsMarshalling.buildDelegateParam( pi, pb );
			}
		}

		static Type createDelegate( TypeBuilder tbDelegates, MethodInfo method )
		{
			// Initially based on this: https://blogs.msdn.microsoft.com/joelpob/2004/02/15/creating-delegate-types-via-reflection-emit/

			// Create the delegate type
			TypeAttributes ta = TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.NestedPublic;
			TypeBuilder tb = tbDelegates.DefineNestedType( method.Name, ta, typeof( MulticastDelegate ) );
			// Apply [UnmanagedFunctionPointer] using the value from RuntimeClass.defaultCallingConvention
			CustomAttributeBuilder cab = new CustomAttributeBuilder( ciFPAttribute, new object[ 1 ] { RuntimeClass.defaultCallingConvention } );
			tb.SetCustomAttribute( cab );

			// Create constructor for the delegate
			MethodAttributes ma = MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public;
			ConstructorBuilder cb = tb.DefineConstructor( ma, CallingConventions.Standard, new Type[] { typeof( object ), typeof( IntPtr ) } );
			cb.SetImplementationFlags( MethodImplAttributes.Runtime | MethodImplAttributes.Managed );
			cb.DefineParameter( 1, ParameterAttributes.In, "object" );
			cb.DefineParameter( 2, ParameterAttributes.In, "method" );

			// Create Invoke method for the delegate. Appending one more parameter to the start, `[in] IntPtr pThis`
			ParameterInfo[] methodParams = method.GetParameters();
			Type[] paramTypes = new Type[ methodParams.Length + 1 ];
			paramTypes[ 0 ] = typeof( IntPtr );
			for( int i = 0; i < methodParams.Length; i++ )
			{
				ParameterInfo pi = methodParams[ i ];
				Type tp = pi.ParameterType;
				iCustomMarshal cm = pi.customMarshaller();
				if( null != cm )
					tp = cm.getNativeType( pi );
				paramTypes[ i + 1 ] = tp;
			}

			var mb = tb.DefineMethod( "Invoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, typeof( int ), paramTypes );
			mb.SetImplementationFlags( MethodImplAttributes.Runtime | MethodImplAttributes.Managed );
			defineDelegateParameters( mb, methodParams );
			// The method has no code, it's pure virtual.

			return tb.CreateType();
		}

		static readonly object syncRoot = new object();
		static readonly Dictionary<Type, Type[]> delegatesCache = new Dictionary<Type, Type[]>();

		/// <summary>Build static class with the native function pointer delegates, return array of delegate types.
		/// It caches the results because the delegate types are used for both directions of the interop.</summary>
		public static Type[] buildDelegates( Type tInterface )
		{
			lock( syncRoot )
			{
				Type[] result;
				if( delegatesCache.TryGetValue( tInterface, out result ) )
					return result;

				TypeBuilder tbDelegates = createDelegatesType( tInterface );
				// Add delegate types per method
				result = tInterface.GetMethods().Select( mi => createDelegate( tbDelegates, mi ) ).ToArray();
				tbDelegates.CreateType();
				delegatesCache.Add( tInterface, result );

				return result;
			}
		}
	}
}