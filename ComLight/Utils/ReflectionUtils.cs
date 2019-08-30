﻿using System;
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

		public static bool isDelegate( this Type tp )
		{
			return typeof( Delegate ).IsAssignableFrom( tp );
		}

		public static bool hasCustomAttribyte<T>( this Type tp ) where T: Attribute
		{
			return null != tp.GetCustomAttribute<T>();
		}
	}
}