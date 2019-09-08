using ComLight.Marshalling;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ComLight
{
	static class Marshallers
	{
		static readonly object syncRoot = new object();
		static readonly Dictionary<Type, iCustomMarshal> cache = new Dictionary<Type, iCustomMarshal>();

		static iCustomMarshal getMarshaller( Type tp )
		{
			lock( syncRoot )
			{
				iCustomMarshal result;
				if( cache.TryGetValue( tp, out result ) )
					return result;
				result = (iCustomMarshal)Activator.CreateInstance( tp );
				cache.Add( tp, result );
				return result;
			}
		}

		/// <summary>If the parameter type is a COM interface, return instance of InterfaceMarshaller&lt;I&gt;. If the parameter has [Marshaller] attribute, return that one. Otherwise return null.</summary>
		public static iCustomMarshal customMarshaller( this ParameterInfo pi )
		{
			Type tp = pi.ParameterType.unwrapRef();
			if( tp.hasCustomAttribute<ComInterfaceAttribute>() )
			{
				var im = typeof( InterfaceMarshaller<> );
				im = im.MakeGenericType( tp );
				return getMarshaller( im );
			}

			MarshallerAttribute a = pi.GetCustomAttribute<MarshallerAttribute>();
			if( null == a )
				return null;

			return getMarshaller( a.tMarshaller );
		}

		public static bool hasCustomMarshaller( this ParameterInfo pi )
		{
			Type tp = pi.ParameterType.unwrapRef();
			if( tp.hasCustomAttribute<ComInterfaceAttribute>() )
				return true;

			return pi.hasCustomAttribute<MarshallerAttribute>();
		}
	}
}