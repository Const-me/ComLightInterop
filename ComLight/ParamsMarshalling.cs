using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace ComLight
{
	static class ParamsMarshalling
	{
		static readonly ConstructorInfo ciMarshalAs;
		static readonly FieldInfo fiMarshalTypeRef;

		static ParamsMarshalling()
		{
			Type tp = typeof( MarshalAsAttribute );
			ciMarshalAs = tp.GetConstructor( new Type[ 1 ] { typeof( UnmanagedType ) } );
			fiMarshalTypeRef = tp.GetField( "MarshalTypeRef" );
		}

		public static void buildDelegateParam( ParameterInfo source, ParameterBuilder destination )
		{
			// Copy all custom attributes, if any, from source to destination.
			bool hasMarshalAs = false;
			foreach( var ca in source.CustomAttributes )
			{
				var namedFields = ca.NamedArguments.Where( a => a.MemberInfo is FieldInfo ).ToArray();
				FieldInfo[] fields = namedFields.Select( f => (FieldInfo)f.MemberInfo ).ToArray();
				object[] fieldVals = namedFields.Select( f => f.TypedValue.Value ).ToArray();

				var namedProperties = ca.NamedArguments.Where( a => a.MemberInfo is PropertyInfo ).ToArray();
				PropertyInfo[] props = namedProperties.Select( p => (PropertyInfo)p.MemberInfo ).ToArray();
				object[] propVals = namedProperties.Select( p => p.TypedValue.Value ).ToArray();

				object[] ctorArgs = ca.ConstructorArguments.Select( a => a.Value ).ToArray();

				if( ca.Constructor.DeclaringType == typeof( MarshalAsAttribute ) )
					hasMarshalAs = true;

				var cab = new CustomAttributeBuilder( ca.Constructor, ctorArgs, props, propVals, fields, fieldVals );
				destination.SetCustomAttribute( cab );
			}

			// Automatically apply [MarshalAs( UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof( Marshaler<...> ) )] on COM interface arguments
			Type tParamType = source.ParameterType;
			if( tParamType.IsInterface && !hasMarshalAs && null != tParamType.GetCustomAttribute<ComInterfaceAttribute>() )
			{
				// Detected COM interface.
				object[] ctorArgs = new object[ 1 ] { UnmanagedType.CustomMarshaler };
				FieldInfo[] fields = new FieldInfo[ 1 ] { fiMarshalTypeRef };
				Type tMarshaller = typeof( Marshaler<> );
				tMarshaller = tMarshaller.MakeGenericType( tParamType );
				object[] fieldVals = new object[ 1 ] { tMarshaller };
				var cab = new CustomAttributeBuilder( ciMarshalAs, ctorArgs, fields, fieldVals );
				destination.SetCustomAttribute( cab );
			}
		}
	}
}