using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace ComLight.Emit
{
	/// <summary>This static class implements the boilerplate to wrap C++ interfaces into .NET objects.</summary>
	/// <remarks>Most of the heavy-lifting is only done once per interface type.
	/// However, when the interface has methods with custom marshallers, per-instance runtime overhead is not trivial.
	/// The assumption was an object is going to be called many times over its lifetime.</remarks>
	static partial class Proxy
	{
		/// <summary>RuntimeClass.nativePointer</summary>
		static readonly FieldInfo fiNativePointer;
		/// <summary>RuntimeClass..ctor</summary>
		static readonly ConstructorInfo ciRuntimeClassCtor;
		/// <summary>Marshal.GetDelegateForFunctionPointer, the generic one</summary>
		static readonly MethodInfo miGetDelegate;
		/// <summary>ErrorCodes.throwForHR(int)</summary>
		static readonly MethodInfo miThrow;
		/// <summary>Types of argument for the RuntimeClass protected constructor.</summary>
		static readonly Type[] constructorArguments = new Type[ 3 ] { typeof( IntPtr ), typeof( IntPtr[] ), typeof( Guid ) };

		const FieldAttributes privateReadonly = FieldAttributes.Private | FieldAttributes.InitOnly;

		static Proxy()
		{
			Type tBase = typeof( RuntimeClass );
			BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			fiNativePointer = tBase.GetField( "nativePointer", bf );
			ciRuntimeClassCtor = tBase.GetConstructor( constructorArguments );

			Type tMarshal = typeof( Marshal );
			miGetDelegate = tMarshal.GetMethod( "GetDelegateForFunctionPointer", new Type[ 1 ] { typeof( IntPtr ) } );
			Type tCodes = typeof( ErrorCodes );
			miThrow = tCodes.GetMethod( "throwForHR", new Type[ 1 ] { typeof( int ) } );
		}

		/// <summary>Create non-static class for the proxy, it inherits from RuntimeClass, and implements the COM interface.</summary>
		static TypeBuilder createType( Type tInterface )
		{
			string name = tInterface.FullName + "_proxy";
			TypeAttributes attr = TypeAttributes.Public |
					TypeAttributes.Class |
					TypeAttributes.BeforeFieldInit |
					TypeAttributes.AutoLayout;
			Type tBase = typeof( RuntimeClass );
			Type[] interfaces = new Type[ 2 ] { tInterface, typeof( IDisposable ) };
			return Assembly.moduleBuilder.DefineType( name, attr, tBase, interfaces );
		}

		/// <summary>`IntPtr nativeComPointer` parameter expression</summary>
		static readonly ParameterExpression paramNativeObject = Expression.Parameter( typeof( IntPtr ), "nativeComPointer" );

		interface iMethodPrefab
		{
			Type tCtorArg { get; }

			FieldBuilder emitField( TypeBuilder tb );

			void emitConstructorBody( ILGenerator il, int methodIndex, ref int ctorArgIndex, FieldBuilder field );

			void emitMethod( MethodBuilder mb, FieldBuilder field );
		}

		class InterfaceBuilder
		{
			readonly Type tInterface;
			public readonly iMethodPrefab[] prefabs;
			readonly MethodInfo[] methods;

			public int methodsCount => methods.Length;

			public InterfaceBuilder( Type tInterface )
			{
				this.tInterface = tInterface;
				methods = tInterface.GetMethods();
				Type[] tDelegates = NativeDelegates.buildDelegates( tInterface );
				Debug.Assert( methods.Length == tDelegates.Length );
				prefabs = new iMethodPrefab[ methods.Length ];

				for( int i = 0; i < methods.Length; i++ )
				{
					MethodInfo mi = methods[ i ];
					bool anyCustom = mi.GetParameters().Any( Marshallers.hasCustomMarshaller );
					if( anyCustom )
						prefabs[ i ] = new CustomMarshallerMethod( mi, tDelegates[ i ], i );
					else
						prefabs[ i ] = new ProxyMethod( mi, tDelegates[ i ] );
				}
			}

			static void addConstructor( TypeBuilder tb, Type[] argTypes, FieldBuilder[] fields, iMethodPrefab[] prefabs )
			{
				MethodAttributes ma = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
				ConstructorBuilder cb = tb.DefineConstructor( ma, CallingConventions.HasThis, argTypes );
				ILGenerator il = cb.GetILGenerator();

				// Call base constructor
				il.Emit( OpCodes.Ldarg_0 );
				il.Emit( OpCodes.Ldarg_1 );
				il.Emit( OpCodes.Ldarg_2 );
				il.Emit( OpCodes.Ldarg_3 );
				il.Emit( OpCodes.Call, ciRuntimeClassCtor );

				int ctorArgIndex = 4;
				for( int i = 0; i < fields.Length; i++ )
					prefabs[ i ].emitConstructorBody( il, i, ref ctorArgIndex, fields[ i ] );

				il.Emit( OpCodes.Ret );
			}

			static MethodBuilder declareMethod( TypeBuilder tb, MethodInfo method )
			{
				// Method signature
				MethodAttributes ma = MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final;
				string name = method.DeclaringType.FullName + "." + method.Name;
				ParameterInfo[] parameters = method.GetParameters();
				Type[] paramTypes = parameters.Select( pi => pi.ParameterType ).ToArray();
				MethodBuilder mb = tb.DefineMethod( name, ma, method.ReturnType, paramTypes );
				for( int i = 0; i < parameters.Length; i++ )
					mb.DefineParameter( i + 1, parameters[ i ].Attributes, parameters[ i ].Name );

				return mb;
			}

			public Type[] constructorArgumentTypes { get; private set; }

			public Type build()
			{
				buildManagedDelegates( tInterface, prefabs.OfType<CustomMarshallerMethod>() );

				// Create the proxy type
				TypeBuilder tb = createType( tInterface );

				List<Type> ctorArgsList = new List<Type>();
				ctorArgsList.AddRange( constructorArguments );

				// Create readonly fields
				FieldBuilder[] fields = new FieldBuilder[ prefabs.Length ];
				for( int i = 0; i < prefabs.Length; i++ )
				{
					var pf = prefabs[ i ];
					fields[ i ] = pf.emitField( tb );
					Type t = pf.tCtorArg;
					if( null != t )
						ctorArgsList.Add( t );
				}

				// Create the constructor
				constructorArgumentTypes = ctorArgsList.ToArray();
				addConstructor( tb, constructorArgumentTypes, fields, prefabs );

				// Create methods
				for( int i = 0; i < methods.Length; i++ )
				{
					MethodBuilder mb = declareMethod( tb, methods[ i ] );
					prefabs[ i ].emitMethod( mb, fields[ i ] );
					tb.DefineMethodOverride( mb, methods[ i ] );
				}

				// Finalize the proxy type
				return tb.CreateType();
			}

			public bool anyCustomMethods => prefabs.OfType<CustomMarshallerMethod>().Any();
			public CustomMarshallerMethod[] customMethods => prefabs.OfType<CustomMarshallerMethod>().ToArray();
		}

		/// <summary>Build class factory function for proxy type which doesn't use any custom marshalers.</summary>
		static Func<IntPtr, object> buildSimpleFactory( Type tProxy, Expression[] baseCtorArgs )
		{
			ConstructorInfo ci = tProxy.GetConstructor( constructorArguments );
			var eNew = Expression.New( ci, baseCtorArgs );
			Expression eCast = Expression.Convert( eNew, typeof( object ) );
			Expression<Func<IntPtr, object>> lambda = Expression.Lambda<Func<IntPtr, object>>( eCast, paramNativeObject );
			return lambda.Compile();
		}

		/// <summary>Build class factory function for a proxy type which does use custom marshallers.</summary>
		static Func<IntPtr, IntPtr[], Delegate[], object> buildCustomFactory( Type tProxy, Expression[] baseCtorArgs, InterfaceBuilder ib )
		{
			ConstructorInfo ci = tProxy.GetConstructor( ib.constructorArgumentTypes );
			ParameterExpression peVtbl = Expression.Parameter( typeof( IntPtr[] ), "vtable" );
			ParameterExpression peDelegates = Expression.Parameter( typeof( Delegate[] ), "delegates" );

			CustomMarshallerMethod[] customMethods = ib.customMethods;
			Expression[] ctorArgs = new Expression[ baseCtorArgs.Length + customMethods.Length ];
			Array.Copy( baseCtorArgs, ctorArgs, baseCtorArgs.Length );
			for( int i = 0; i < customMethods.Length; i++ )
			{
				Expression eItem = Expression.ArrayIndex( peDelegates, Expression.Constant( i ) );
				eItem = Expression.Convert( eItem, customMethods[ i ].tManagedDelegate );
				ctorArgs[ baseCtorArgs.Length + i ] = eItem;
			}
			var eNew = Expression.New( ci, ctorArgs );
			Expression eCast = Expression.Convert( eNew, typeof( object ) );
			var lambda = Expression.Lambda<Func<IntPtr, IntPtr[], Delegate[], object>>( eCast, paramNativeObject, peVtbl, peDelegates );
			return lambda.Compile();
		}

		public static Func<IntPtr, object> build( Type tInterface )
		{
			Guid iid = ReflectionUtils.checkInterface( tInterface );
			InterfaceBuilder ib = new InterfaceBuilder( tInterface );
			Type tProxy = ib.build();

			int methodsCount = ib.methodsCount;
			Expression<Func<IntPtr, IntPtr[]>> eReadVtbl = ( IntPtr nativeComPointer ) => RuntimeClass.readVirtualTable( nativeComPointer, methodsCount );
			ConstantExpression eIid = Expression.Constant( iid, typeof( Guid ) );
			Expression[] baseCtorArgs = new Expression[ 3 ]
			{
				paramNativeObject,
				Expression.Invoke( eReadVtbl, paramNativeObject ),
				eIid
			};

			if( !ib.anyCustomMethods )
			{
				// Simple case here, no custom marshalers. No need to do late binding, just compile the factory that creates new proxy
				return buildSimpleFactory( tProxy, baseCtorArgs );
			}

			var customFactory = buildCustomFactory( tProxy, baseCtorArgs, ib );

			var lateBinder = createLateBinder( ib.customMethods );

			return ( IntPtr nativeComPointer ) =>
			{
				IntPtr[] vtable = RuntimeClass.readVirtualTable( nativeComPointer, methodsCount );
				Delegate[] marshallers = lateBinder( nativeComPointer, vtable );
				return customFactory( nativeComPointer, vtable, marshallers );
			};
		}
	}
}