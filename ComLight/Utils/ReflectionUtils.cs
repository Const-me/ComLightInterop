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
			{
				if( m.IsGenericMethod || m.IsGenericMethodDefinition )
					throw new ArgumentException( $"The interface method { tp.FullName }.{ m.Name } is generic, this is not supported" );

				Type tRet = m.ReturnType;
				if( tRet == typeof( int ) || tRet == typeof( void ) )
					continue;
				throw new ArgumentException( $"The interface method { tp.FullName }.{ m.Name } has unsupported return type { tRet.FullName }, must be int or void" );
			}

			return attribute.iid;
		}
	}
}