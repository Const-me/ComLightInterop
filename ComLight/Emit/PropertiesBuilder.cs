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
		public void implement( TypeBuilder typeBuilder, string name, MethodBuilder methodBuilder )
		{
			if( dict.TryGetValue( name, out var mi ) )
				typeBuilder.DefineMethodOverride( methodBuilder, mi );
		}
	}
}