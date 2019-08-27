using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq.Expressions;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace ComLight
{
	static class WrapInterface
	{
		static readonly AssemblyBuilder assemblyBuilder;
		static readonly ModuleBuilder moduleBuilder;

		/// <summary>RuntimeClass.nativePointer</summary>
		static readonly FieldInfo fiNativePointer;
		/// <summary>RuntimeClass..ctor</summary>
		static readonly ConstructorInfo ciRuntimeClassCtor;
		/// <summary>RuntimeClass.readVirtualTable</summary>
		static readonly MethodInfo miReadVtbl;
		/// <summary>Marshal.GetDelegateForFunctionPointer, the generic one</summary>
		static readonly MethodInfo miGetDelegate;
		/// <summary>Marshal.ThrowExceptionForHR(int)</summary>
		static readonly MethodInfo miThrow;
		/// <summary>Types of argument for RuntimeClass, same for generated proxies.</summary>
		static readonly Type[] constructorArguments = new Type[ 3 ] { typeof( IntPtr ), typeof( IntPtr[] ), typeof( Guid ) };
		/// <summary>Constructor of UnmanagedFunctionPointerAttribute</summary>
		static readonly ConstructorInfo ciFPAttribute;

		static WrapInterface()
		{
			// Create dynamic assembly builder, and cache some reflected stuff we use to build these proxies in runtime.
			var an = new AssemblyName( "ComLight.Wrappers" );

#if NETCOREAPP
			assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly( an, AssemblyBuilderAccess.Run );
			moduleBuilder = assemblyBuilder.DefineDynamicModule( "MainModule" );
#else
			assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly( an, AssemblyBuilderAccess.RunAndSave );
			moduleBuilder = assemblyBuilder.DefineDynamicModule( "MainModule", an.Name + ".dll" );
#endif

			Type tBase = typeof( RuntimeClass );
			BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			fiNativePointer = tBase.GetField( "nativePointer", bf );
			ciRuntimeClassCtor = tBase.GetConstructor( constructorArguments );
			miReadVtbl = tBase.GetMethod( "readVirtualTable", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static );

			Type tMarshal = typeof( Marshal );
			miGetDelegate = tMarshal.GetMethod( "GetDelegateForFunctionPointer", new Type[ 1 ] { typeof( IntPtr ) } );
			miThrow = tMarshal.GetMethod( "ThrowExceptionForHR", new Type[ 1 ] { typeof( int ) } );

			Type tPointerAttr = typeof( UnmanagedFunctionPointerAttribute );
			ciFPAttribute = tPointerAttr.GetConstructor( new Type[ 1 ] { typeof( CallingConvention ) } );
		}

		/// <summary>Implement constructor for the proxy type</summary>
		static void addConstructor( TypeBuilder tb, FieldBuilder[] delegateFields )
		{
			MethodAttributes ma = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
			ConstructorBuilder cb = tb.DefineConstructor( ma, CallingConventions.HasThis, constructorArguments );
			ILGenerator il = cb.GetILGenerator();
			// Call base constructor
			il.Emit( OpCodes.Ldarg_0 );
			il.Emit( OpCodes.Ldarg_1 );
			il.Emit( OpCodes.Ldarg_2 );
			il.Emit( OpCodes.Ldarg_3 );
			il.Emit( OpCodes.Call, ciRuntimeClassCtor );

			// Initialize delegates readonly fields, by calling Marshal.GetDelegateForFunctionPointer
			for( int i = 0; i < delegateFields.Length; i++ )
			{
				il.Emit( OpCodes.Ldarg_0 );
				il.Emit( OpCodes.Ldarg_2 );
				il.pushIntConstant( i + 3 );
				il.Emit( OpCodes.Ldelem_I );
				// Specialize the generic Marshal.GetDelegateForFunctionPointer method with the delegate type
				MethodInfo mi = miGetDelegate.MakeGenericMethod( delegateFields[ i ].FieldType );
				il.Emit( OpCodes.Call, mi );
				// Store delegate in the readonly field
				il.Emit( OpCodes.Stfld, delegateFields[ i ] );
			}

			il.Emit( OpCodes.Ret );
		}

		static void implementMethod( TypeBuilder tb, MethodInfo method, FieldBuilder field )
		{
			// Method signature
			MethodAttributes ma = MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final;
			string name = method.DeclaringType.FullName + "." + method.Name;
			ParameterInfo[] parameters = method.GetParameters();
			Type[] paramTypes = parameters.Select( pi => pi.ParameterType ).ToArray();
			MethodBuilder mb = tb.DefineMethod( name, ma, method.ReturnType, paramTypes );
			for( int i = 0; i < parameters.Length; i++ )
				mb.DefineParameter( i + 1, parameters[ i ].Attributes, parameters[ i ].Name );

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
				// Call Marshal.ThrowExceptionForHR
				il.EmitCall( OpCodes.Call, miThrow, null );
			}

			il.Emit( OpCodes.Ret );

			tb.DefineMethodOverride( mb, method );
		}

		/// <summary>Create static class with the delegates</summary>
		static TypeBuilder createDelegatesType( Type tInterface )
		{
			string name = tInterface.Name + "_delegates";
			TypeAttributes attr = TypeAttributes.Class |
				TypeAttributes.Abstract | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.AutoClass | TypeAttributes.BeforeFieldInit;
			return moduleBuilder.DefineType( name, attr );
		}

		/// <summary>Create non-static class for the proxy, it inherits from RuntimeClass, and implements the COM interface.</summary>
		static TypeBuilder createType( Type tInterface )
		{
			string name = tInterface.Name + "_proxy";
			TypeAttributes attr = TypeAttributes.Public |
					TypeAttributes.Class |
					TypeAttributes.BeforeFieldInit |
					TypeAttributes.AutoLayout;
			Type tBase = typeof( RuntimeClass );
			Type[] interfaces = new Type[ 2 ] { tInterface, typeof( IDisposable ) };
			return moduleBuilder.DefineType( name, attr, tBase, interfaces );
		}

		static void defineDelegateParameters( MethodBuilder mb, ParameterInfo[] methodParams )
		{
			mb.DefineParameter( 1, ParameterAttributes.In, "pThis" );
			for( int i = 0; i < methodParams.Length; i++ )
			{
				ParameterInfo pi = methodParams[ i ];
				ParameterBuilder pb = mb.DefineParameter( i + 2, pi.Attributes, pi.Name );
				EmitUtils.copyCustomAttributes( pi, pb );
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
				paramTypes[ i + 1 ] = methodParams[ i ].ParameterType;

			var mb = tb.DefineMethod( "Invoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, typeof( int ), paramTypes );
			mb.SetImplementationFlags( MethodImplAttributes.Runtime | MethodImplAttributes.Managed );
			defineDelegateParameters( mb, methodParams );
			// The method has no code, it's pure virtual.

			return tb.CreateType();
		}

		static Func<IntPtr, object> buildFactory( Type tpWrapper, int methodsCount, Guid iid )
		{
			// For this part we don't need to mess with opcodes, Linq.Expression is much higher level and works just fine.
			var eParam = Expression.Parameter( typeof( IntPtr ), "nativeComPointer" );
			var eVtbl = Expression.Call( miReadVtbl, eParam, Expression.Constant( methodsCount, typeof( int ) ) );
			var eIid = Expression.Constant( iid, typeof( Guid ) );
			ConstructorInfo ci = tpWrapper.GetConstructor( constructorArguments );
			var eNew = Expression.New( ci, eParam, eVtbl, eIid );
			Expression eCast = Expression.Convert( eNew, typeof( object ) );

			Expression<Func<IntPtr, object>> lambda = Expression.Lambda<Func<IntPtr, object>>( eCast, eParam );
			return lambda.Compile();
		}

#if NETCOREAPP
		static void dbgSaveAssembly() { }
#else
		static void dbgSaveAssembly()
		{
			string name = assemblyBuilder.GetName().Name + ".dll";
			assemblyBuilder.Save( name );
		}
#endif

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

		/// <summary>Build proxy class which allows C# to consume the specified native interface, return a class factory function to create an instance of that class.</summary>
		public static Func<IntPtr, object> build( Type tInterface )
		{
			Guid iid = ReflectionUtils.checkInterface( tInterface );

			Type[] delegates = buildDelegates( tInterface );

			// Create the proxy type
			TypeBuilder tb = createType( tInterface );

			// Add readonly fields per method. This step requires delegate types to be built already, that's why we use 2 classes, the static one with delegates, and the proxy.
			MethodInfo[] methods = tInterface.GetMethods();
			FieldBuilder[] fields = new FieldBuilder[ methods.Length ];
			for( int i = 0; i < methods.Length; i++ )
			{
				FieldAttributes fa = FieldAttributes.Private | FieldAttributes.InitOnly;
				FieldBuilder fb = tb.DefineField( "m_" + methods[ i ].Name, delegates[ i ], fa );
				fields[ i ] = fb;
			}

			// Add constructor, it initializes all readonly fields.
			addConstructor( tb, fields );

			// Implement interface methods
			for( int i = 0; i < methods.Length; i++ )
				implementMethod( tb, methods[ i ], fields[ i ] );

			// Finally, build the factory function.
			Func<IntPtr, object> result = buildFactory( tb.CreateType(), methods.Length, iid );

			// Debug code, save the generated assembly
			// Only works on Windows due to this issue from 2015, still not fixed: https://github.com/dotnet/corefx/issues/4491
			dbgSaveAssembly();

			return result;
		}
	}
}