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

		static void addMethod( ref Dictionary<string, List<MethodInfo>> result, string key, MethodInfo val )
		{
			List<MethodInfo> list;

			if( null == result )
			{
				result = new Dictionary<string, List<MethodInfo>>( namesComparer );
				list = new List<MethodInfo>();
				list.Add( val );
				result.Add( key, list );
				return;
			}

			if( result.TryGetValue( key, out list ) )
			{
				list.Add( val );
				return;
			}
			list = new List<MethodInfo>();
			list.Add( val );
			result.Add( key, list );
		}

		static void reflect( ref Dictionary<string, List<MethodInfo>> result, PropertyInfo[] properties )
		{
			foreach( var pi in properties )
			{
				MethodNames names = new MethodNames( pi );
				MethodInfo getter = pi.GetGetMethod(), setter = pi.GetSetMethod();
				if( null != getter )
					addMethod( ref result, names.getterMethod, getter );
				if( null != setter )
					addMethod( ref result, names.setterMethod, setter );
			}
		}

		// Key = method name, value = list of getters or setters which need implementing
		readonly Dictionary<string, List<MethodInfo>> dict;

		public static PropertiesBuilder createIfNeeded( Type tInterface )
		{
			Dictionary<string, List<MethodInfo>> dict = null;

			reflect( ref dict, tInterface.GetProperties() );

			foreach( Type baseIface in tInterface.GetInterfaces() )
				reflect( ref dict, baseIface.GetProperties() );

			if( null == dict || dict.Count <= 0 )
				return null;

			return new PropertiesBuilder( dict );
		}

		PropertiesBuilder( Dictionary<string, List<MethodInfo>> dict )
		{
			this.dict = dict;
		}

		public void implement( TypeBuilder typeBuilder, string name, MethodBuilder methodBuilder )
		{
			if( !dict.TryGetValue( name, out var list ) )
				return;
			foreach( MethodInfo mi in list )
				typeBuilder.DefineMethodOverride( methodBuilder, mi );
		}
	}
}