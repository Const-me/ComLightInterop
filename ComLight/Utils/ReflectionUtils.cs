using System;
using System.Reflection;

namespace ComLight
{
	static class ReflectionUtils
	{
		/// <summary>Verify COM interface is OK, return it's GUID value.</summary>
		public static Guid checkInterface( Type tp )
		{
			if( !tp.IsInterface )
				throw new ArgumentException( $"Marshaler type argument { tp.FullName } is not an interface" );

			if( !tp.IsPublic )
			{
				// Proxies are implemented in different assembly, a dynamic one, they need access to the interface
				throw new ArgumentException( $"COM interface { tp.FullName } is not public" );
			}

			if( tp.IsGenericType || tp.IsConstructedGenericType )
			{
				throw new ArgumentException( $"COM interface { tp.FullName } is generic, this is not supported" );
			}

			ComInterfaceAttribute attribute = tp.GetCustomAttribute<ComInterfaceAttribute>();
			if( null == attribute )
				throw new ArgumentException( $"COM interface { tp.FullName } doesn't have [ComInterface] attribute applied" );

			foreach( var m in tp.GetMethods() )
				ParamsMarshalling.checkInterfaceMethod( m );

			return attribute.iid;
		}

		/// <summary>true if the type inherits from System.Delegate</summary>
		public static bool isDelegate( this Type tp )
		{
			return typeof( Delegate ).IsAssignableFrom( tp );
		}

		/// <summary>true if the type has specific custom attribute applied</summary>
		public static bool hasCustomAttribute<T>( this Type tp ) where T : Attribute
		{
			return null != tp.GetCustomAttribute<T>();
		}

		/// <summary>true if the parameter has specific custom attribute applied</summary>
		public static bool hasCustomAttribute<T>( this ParameterInfo pi ) where T : Attribute
		{
			return null != pi.GetCustomAttribute<T>();
		}

		public static bool hasRetValIndex( this MethodInfo mi )
		{
			return null != mi.GetCustomAttribute<RetValIndexAttribute>();
		}
	}
}