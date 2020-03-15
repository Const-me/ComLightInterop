using ComLight.Marshalling;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ComLight
{
	static class Marshallers
	{
		// iCustomMarshal instances aren't supposed to have any state. For better performance, caching them on the hash map.
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

		/// <summary>If the parameter type is an array of COM interfaces, returns type of that interface; otherwise returns null.</summary>
		static Type interfaceArrayElementType( this Type tParameter )
		{
			if( !tParameter.IsArray )
				return null;
			Type tElement = tParameter.GetElementType();
			if( !tElement.hasCustomAttribute<ComInterfaceAttribute>() )
				return null;

			if( 1 != tParameter.GetArrayRank() )
				throw new ApplicationException( "Trying to marshal multi-dimensional array of COM objects. ComLight runtime doesn't support that." );
			return tElement;
		}

		/// <summary>If the parameter type is a COM interface, return instance of InterfaceMarshaller&lt;I&gt;.
		/// If the parameter has [Marshaller] attribute, return that one.
		/// If the parameter is an array of COM interfaces, return instance of InterfaceArrayMarshaller&lt;I&gt;.
		/// Otherwise return null.</summary>
		public static iCustomMarshal customMarshaller( this ParameterInfo pi )
		{
			Type tp = pi.ParameterType.unwrapRef();

			if( tp.hasCustomAttribute<ComInterfaceAttribute>() )
			{
				var im = typeof( InterfaceMarshaller<> );
				im = im.MakeGenericType( tp );
				return getMarshaller( im );
			}

			if( tp.interfaceArrayElementType() is Type tElement )
			{
				var iam = typeof( InterfaceArrayMarshaller<> );
				iam = iam.MakeGenericType( tElement );
				return getMarshaller( iam );
			}

			if( pi.GetCustomAttribute<MarshallerAttribute>() is MarshallerAttribute a )
				return getMarshaller( a.tMarshaller );

			return null;
		}

		public static bool hasCustomMarshaller( this ParameterInfo pi )
		{
			Type tp = pi.ParameterType.unwrapRef();
			if( tp.hasCustomAttribute<ComInterfaceAttribute>() )
				return true;
			if( pi.hasCustomAttribute<MarshallerAttribute>() )
				return true;
			// COM interface arrays don't have any special attributes applied in the C# code of the source interface, yet they need custom marshaling as well.
			if( null != tp.interfaceArrayElementType() )
				return true;
			return false;
		}
	}
}