using System;
using System.Collections.Generic;
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
		static readonly FieldInfo fiSizeParamIndex;
		static readonly FieldInfo fiSizeConst;

		static readonly ConstructorInfo ciInAttribute;

		static ParamsMarshalling()
		{
			Type tp = typeof( MarshalAsAttribute );
			ciMarshalAs = tp.GetConstructor( new Type[ 1 ] { typeof( UnmanagedType ) } );
			fiMarshalTypeRef = tp.GetField( "MarshalTypeRef" );
			fiSizeParamIndex = tp.GetField( "SizeParamIndex" );
			fiSizeConst = tp.GetField( "SizeConst" );

			tp = typeof( InAttribute );
			ciInAttribute = tp.GetConstructor( Type.EmptyTypes );
		}

		[Flags]
		enum eDirection: byte
		{
			None = 0,
			In = 1,
			Out = 2
		};

		static void updateDirection( Type tAttribute, ref eDirection dir )
		{
			if( tAttribute == typeof( InAttribute ) )
				dir |= eDirection.In;
			else if( tAttribute == typeof( OutAttribute ) )
				dir |= eDirection.Out;
		}

		static V valueOrDefault<K, V>( this Dictionary<K, V> dict, K key )
		{
			V val;
			if( dict.TryGetValue( key, out val ) )
				return val;
			return default( V );
		}

		static bool buildMarshalAsAttribute( ParameterInfo source, ParameterBuilder destination, CustomAttributeData ca )
		{
			MarshalAsAttribute maa = source.GetCustomAttribute<MarshalAsAttribute>();
			if( maa.Value != UnmanagedType.LPArray )
				return false;
			object[] ctorArgs = new object[ 1 ] { maa.Value };

			Dictionary<FieldInfo, object> dictOld = ca.NamedArguments.Where( a => a.IsField )
				.ToDictionary( a => (FieldInfo)a.MemberInfo, a => a.TypedValue.Value );

			Dictionary<FieldInfo, object> dictNew = new Dictionary<FieldInfo, object>();

			object obj = dictOld.valueOrDefault( fiSizeParamIndex );
			if( null != obj )
			{
				short idx = (short)obj;
				idx++;
				dictNew[ fiSizeParamIndex ] = idx;
			}
			else if( dictOld.ContainsKey( fiSizeConst ) && (int)dictOld[ fiSizeConst ] > 0 )
			{
				dictNew[ fiSizeConst ] = dictOld[ fiSizeConst ];
			}
			else
				throw new ArgumentException( "When marshaling writable arrays, you must specify either SizeParamIndex or SizeConst" );

			var cab = new CustomAttributeBuilder( ciMarshalAs, ctorArgs, dictNew.Keys.ToArray(), dictNew.Values.ToArray() );
			destination.SetCustomAttribute( cab );
			return true;
		}

		public static void buildDelegateParam( ParameterInfo source, ParameterBuilder destination )
		{
			var cm = source.customMarshaller();
			if( null != cm )
			{
				cm.applyDelegateParams( source, destination );
				return;
			}

			bool hasMarshalAs = false;
			eDirection dir = eDirection.None;

			// Copy all custom attributes, if any, from source to destination.
			foreach( var ca in source.CustomAttributes )
			{
				Type tAttribute = ca.Constructor.DeclaringType;
				if( tAttribute == typeof( MarshalAsAttribute ) )
				{
					hasMarshalAs = true;
					if( buildMarshalAsAttribute( source, destination, ca ) )
						continue;
				}
				updateDirection( tAttribute, ref dir );

				var namedFields = ca.NamedArguments.Where( a => a.MemberInfo is FieldInfo ).ToArray();
				FieldInfo[] fields = namedFields.Select( f => (FieldInfo)f.MemberInfo ).ToArray();
				object[] fieldVals = namedFields.Select( f => f.TypedValue.Value ).ToArray();

				var namedProperties = ca.NamedArguments.Where( a => a.MemberInfo is PropertyInfo ).ToArray();
				PropertyInfo[] props = namedProperties.Select( p => (PropertyInfo)p.MemberInfo ).ToArray();
				object[] propVals = namedProperties.Select( p => p.TypedValue.Value ).ToArray();
				object[] ctorArgs = ca.ConstructorArguments.Select( a => a.Value ).ToArray();

				var cab = new CustomAttributeBuilder( ca.Constructor, ctorArgs, props, propVals, fields, fieldVals );
				destination.SetCustomAttribute( cab );
			}

			// When user has already specified [MarshalAs] for that parameter, respect the choice, they probably know what they're doing.
			if( hasMarshalAs )
				return;

			Type tParamType = source.ParameterType;
			if( tParamType.IsInterface && null != tParamType.GetCustomAttribute<ComInterfaceAttribute>() )
			{
				// Detected COM interface. Automatically apply [MarshalAs( UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof( Marshaler<...> ) )] on the argument
				object[] ctorArgs = new object[ 1 ] { UnmanagedType.CustomMarshaler };
				FieldInfo[] fields = new FieldInfo[ 1 ] { fiMarshalTypeRef };
				Type tMarshaller = typeof( Marshaler<> );
				tMarshaller = tMarshaller.MakeGenericType( tParamType );
				object[] fieldVals = new object[ 1 ] { tMarshaller };
				var cab = new CustomAttributeBuilder( ciMarshalAs, ctorArgs, fields, fieldVals );
				destination.SetCustomAttribute( cab );
			}

			if( tParamType.IsArray )
			{
				// Detected array argument, apply [MarshalAs( UnmanagedType.LPArray )]
				object[] ctorArgs = new object[ 1 ] { UnmanagedType.LPArray };
				var cab = new CustomAttributeBuilder( ciMarshalAs, ctorArgs );
				destination.SetCustomAttribute( cab );

				if( dir == eDirection.None )
				{
					// When no direction is specified in the COM interface, default to [In] because it's faster
					cab = new CustomAttributeBuilder( ciInAttribute, new object[ 0 ] );
					destination.SetCustomAttribute( cab );
				}
			}
		}
	}
}