using System;
using System.Collections.Generic;
using System.Reflection;

namespace ComLight
{
	static class Marshallers
	{
		static readonly object syncRoot = new object();
		static readonly Dictionary<Type, iCustomMarshal> cache = new Dictionary<Type, iCustomMarshal>();

		public static iCustomMarshal customMarshaller( this ParameterInfo pi )
		{
			MarshallerAttribute a = pi.GetCustomAttribute<MarshallerAttribute>();
			if( null == a )
				return null;

			Type t = a.tMarshaller;
			lock( syncRoot )
			{
				iCustomMarshal result;
				if( cache.TryGetValue( t, out result ) )
					return result;
				result = (iCustomMarshal)Activator.CreateInstance( t );
				cache.Add( t, result );
				return result;
			}
		}
	}
}