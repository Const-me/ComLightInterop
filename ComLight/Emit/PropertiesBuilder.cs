using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ComLight.Emit
{
	class PropertiesBuilder
	{
		static readonly IEqualityComparer<string> namesComparer = StringComparer.InvariantCultureIgnoreCase;

		struct MethodNames
		{
			public readonly string getterMethod, setterMethod;

			public MethodNames( PropertyInfo pi )
			{
				var attrib = pi.GetCustomAttribute<PropertyAttribute>();
				if( null == attrib )
				{
					getterMethod = "get" + pi.Name;
					setterMethod = "set" + pi.Name;
				}
				else
				{
					getterMethod = attrib.getterMethod;
					setterMethod = attrib.setterMethod;
				}
			}
		}

		static void addMethod( ref Dictionary<string, MethodInfo> result, string key, MethodInfo val, Type ifaceBuild )
		{
			if( null == result )
			{
				result = new Dictionary<string, MethodInfo>( namesComparer );
				result.Add( key, val );
				return;
			}

			if( result.ContainsKey( key ) )
			{
				throw new ArgumentException( $"COM interface { ifaceBuild.FullName } has multiple properties implemented by the same method { key }. This is not supported." );
				// It's easy to support BTW, but I don't see any good reason to.
				// Why would you want different properties doing exactly same thing?
			}
			result.Add( key, val );
		}

		static void reflect( ref Dictionary<string, MethodInfo> result, Type ifaceReflect, Type ifaceBuild )
		{
			PropertyInfo[] properties = ifaceReflect.GetProperties();
			foreach( var pi in properties )
			{
				MethodNames names = new MethodNames( pi );
				MethodInfo getter = pi.GetGetMethod(), setter = pi.GetSetMethod();
				if( null != getter )
					addMethod( ref result, names.getterMethod, getter, ifaceBuild );
				if( null != setter )
					addMethod( ref result, names.setterMethod, setter, ifaceBuild );
			}
		}

		// Key = COM method name, value = getters or setter which need implementing.
		readonly Dictionary<string, MethodInfo> dict;

		public static PropertiesBuilder createIfNeeded( Type tInterface )
		{
			Dictionary<string, MethodInfo> dict = null;

			reflect( ref dict, tInterface, tInterface );

			foreach( Type baseIface in tInterface.GetInterfaces() )
				reflect( ref dict, baseIface, tInterface );

			if( null == dict )
				return null;

			return new PropertiesBuilder( dict );
		}

		PropertiesBuilder( Dictionary<string, MethodInfo> dict )
		{
			this.dict = dict;
		}

		// Need name parameter because MethodBuilder.Name is fully qualified, `Namespace.iInterface.getSomeProperty`, and we just need `getSomeProperty` here
		public void implement( TypeBuilder typeBuilder, MethodInfo miMethod, MethodBuilder methodBuilder )
		{
			if( !dict.TryGetValue( miMethod.Name, out var mi ) )
				return;
			implementProperty( typeBuilder, miMethod, methodBuilder, mi );
			// typeBuilder.DefineMethodOverride( methodBuilder, mi );
		}

		static readonly Type[] noTypes = new Type[ 0 ];

		const MethodAttributes methodAttributes = MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final;

		void implementProperty( TypeBuilder typeBuilder, MethodInfo comMethod, MethodBuilder methodBuilder, MethodInfo propertyMethod )
		{
			var mp = comMethod.GetParameters();

			if( propertyMethod.Name.StartsWith( "get_" ) )
			{
				if( mp.Length == 0 )
				{
					// The COM method doesn't accept any parameters.
					// We don't need to build any extra methods.
					if( comMethod.ReturnType != propertyMethod.ReturnType )
						throw new ArgumentException( $"Property getter { propertyMethod.Name } has return type { propertyMethod.ReturnType.FullName }, while the COM method { comMethod.Name } returns { comMethod.ReturnType.FullName }. They must be the same." );
					typeBuilder.DefineMethodOverride( methodBuilder, propertyMethod );
					return;
				}

				// The COM method has parameters. It must be exactly one then, output.
				// Build a small getter method with 1 local variable.
				if( mp.Length != 1 || !mp[ 0 ].IsOut )
					throw new ArgumentException( $"COM method { comMethod.Name } can't implement { propertyMethod.Name }, the COM method must take a single out argument" );
				if( mp[ 0 ].ParameterType != propertyMethod.ReturnType.MakeByRefType() )
					throw new ArgumentException( $"COM method { comMethod.Name } can't implement { propertyMethod.Name }, the types are different." );

				MethodBuilder mb = typeBuilder.DefineMethod( propertyMethod.Name, methodAttributes, propertyMethod.ReturnType, noTypes );
				ILGenerator il = mb.GetILGenerator();
				LocalBuilder res = il.DeclareLocal( propertyMethod.ReturnType );
				il.Emit( OpCodes.Ldarg_0 );
				il.Emit( OpCodes.Ldloca_S, res );
				il.Emit( OpCodes.Call, methodBuilder );
				il.Emit( OpCodes.Pop );
				il.Emit( OpCodes.Ldloc_0 );
				il.Emit( OpCodes.Ret );

				typeBuilder.DefineMethodOverride( mb, propertyMethod );
				return;
			}

			if( propertyMethod.Name.StartsWith( "set_" ) )
			{
				if( mp.Length != 1 )
					throw new ArgumentException( $"COM method { comMethod.Name } can't implement { propertyMethod.Name }, the COM method must take a single argument" );

				ParameterInfo piProperty = propertyMethod.GetParameters()[ 0 ];
				if( mp[ 0 ].ParameterType == piProperty.ParameterType )
				{
					// Parameter types match. We don't need to build any extra methods.
					typeBuilder.DefineMethodOverride( methodBuilder, propertyMethod );
					return;
				}

				// The COM method is like void setSomething( [In] ref something )
				// Build a small setter method with slightly different signature, without the `ref`
				if( mp[ 0 ].ParameterType != piProperty.ParameterType.MakeByRefType() )
					throw new ArgumentException( $"COM method { comMethod.Name } can't implement { propertyMethod.Name }, the types are different." );

				MethodBuilder mb = typeBuilder.DefineMethod( propertyMethod.Name, methodAttributes, typeof( void ), new Type[ 1 ] { piProperty.ParameterType } );
				mb.DefineParameter( 1, ParameterAttributes.In, "value" );

				ILGenerator il = mb.GetILGenerator();
				il.Emit( OpCodes.Ldarg_0 );
				il.Emit( OpCodes.Ldarga_S, (byte)0 );
				il.Emit( OpCodes.Call, methodBuilder );
				il.Emit( OpCodes.Pop );
				il.Emit( OpCodes.Ret );

				typeBuilder.DefineMethodOverride( mb, propertyMethod );
				return;
			}

			throw new ArgumentException( "Unexpected property method " + propertyMethod.Name );
		}
	}
}