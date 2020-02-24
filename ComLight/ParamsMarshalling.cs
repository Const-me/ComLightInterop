using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

namespace ComLight
{
	/// <summary>Applies marshalling attributes while building native delegates for interface methods</summary>
	static class ParamsMarshalling
	{
		static UnmanagedType getNativeStringType()
		{
			if( Environment.OSVersion.Platform == PlatformID.Win32NT )
				return UnmanagedType.LPWStr;
			return UnmanagedType.LPUTF8Str;
		}

		static readonly object[] nativeStringType = new object[ 1 ] { getNativeStringType() };
		static readonly object[] emptyObjectArray = new object[ 0 ];

		static readonly ConstructorInfo ciMarshalAs;
		static readonly FieldInfo fiMarshalTypeRef;
		static readonly FieldInfo fiSizeParamIndex;
		static readonly FieldInfo fiSizeConst;

		static readonly ConstructorInfo ciInAttribute;
		static readonly ConstructorInfo ciOutAttribute;

		static ParamsMarshalling()
		{
			Type tp = typeof( MarshalAsAttribute );
			ciMarshalAs = tp.GetConstructor( new Type[ 1 ] { typeof( UnmanagedType ) } );
			fiMarshalTypeRef = tp.GetField( "MarshalTypeRef" );
			fiSizeParamIndex = tp.GetField( "SizeParamIndex" );
			fiSizeConst = tp.GetField( "SizeConst" );

			tp = typeof( InAttribute );
			ciInAttribute = tp.GetConstructor( Type.EmptyTypes );

			tp = typeof( OutAttribute );
			ciOutAttribute = tp.GetConstructor( Type.EmptyTypes );
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

		/// <summary>Apply [In] attribute to the parameter</summary>
		public static void applyInAttribute( this ParameterBuilder pb )
		{
			var cab = new CustomAttributeBuilder( ciInAttribute, emptyObjectArray );
			pb.SetCustomAttribute( cab );
		}

		/// <summary>Apply [Out] attribute to the parameter</summary>
		public static void applyOutAttribute( this ParameterBuilder pb )
		{
			var cab = new CustomAttributeBuilder( ciOutAttribute, emptyObjectArray );
			pb.SetCustomAttribute( cab );
		}

		static bool applyCustomMarshalling( ParameterInfo source, ParameterBuilder destination )
		{
			// [Marshaller] attribute, or when the type is a COM interface.
			var cm = source.customMarshaller();
			if( null != cm )
			{
				cm.applyDelegateParams( source, destination );
				return true;
			}

			// [NativeString]
			if( source.hasCustomAttribute<NativeStringAttribute>() )
			{
				// Apply [MarshalAs] attribute specifying native string type, LPWStr on Windows, LPUTF8Str on Linux.
				var cab = new CustomAttributeBuilder( ciMarshalAs, nativeStringType );
				destination.SetCustomAttribute( cab );

				// Copy In & Out attributes, if any.
				if( source.hasCustomAttribute<InAttribute>() )
					destination.applyInAttribute();
				if( source.hasCustomAttribute<OutAttribute>() )
					destination.applyOutAttribute();

				return true;
			}

			return false;
		}

		static bool isComInterface( this ParameterInfo source )
		{
			return source.ParameterType.unwrapRef().hasCustomAttribute<ComInterfaceAttribute>();
		}

		static void checkParameter( ParameterInfo source )
		{
			var cm = source.customMarshaller();
			if( null != cm )
			{
				cm.getNativeType( source );
				return;
			}

			NativeStringAttribute nsa = source.GetCustomAttribute<NativeStringAttribute>();
			if( null != nsa )
			{
				if( source.ParameterType != typeof( string ) && source.ParameterType != typeof( StringBuilder ) )
					throw new ArgumentException( $"[NativeString] must be applied to parameters of type string" );
				return;
			}

			if( source.isComInterface() )
				return;

			if( source.ParameterType.IsByRef )
			{
				Type unwrapped = source.ParameterType.unwrapRef();
				if( unwrapped.isDelegate() )
					throw new ArgumentException( $"You can only pass delegates as input parameters" );
				if( source.IsIn && !unwrapped.IsValueType )
					throw new ArgumentException( $"in/out ref parameters only supported for value types" );
			}

			if( source.ParameterType.isDelegate() )
			{
				if( !source.ParameterType.hasCustomAttribute<UnmanagedFunctionPointerAttribute>() )
					throw new ArgumentException( $"Parameter \"{ source.Name }\" of the method { source.Member.DeclaringType.FullName }.{ source.Member.Name } is a delegate without [UnmanagedFunctionPointer] attribute." );
			}
		}

		public static void checkInterfaceMethod( MethodInfo mi )
		{
			// Ensure the method is not generic
			if( mi.IsGenericMethod || mi.IsGenericMethodDefinition )
				throw new ArgumentException( $"The interface method { mi.DeclaringType.FullName }.{ mi.Name } is generic, this is not supported" );

			// Verify return type
			Type tRet = mi.ReturnType;
			if( tRet != typeof( int ) && tRet != typeof( void ) && tRet != typeof( bool ) )
				throw new ArgumentException( $"The interface method { mi.DeclaringType.FullName }.{ mi.Name } has unsupported return type { tRet.FullName }, must be int, void or bool" );

			foreach( var pi in mi.GetParameters() )
				checkParameter( pi );
		}

		public static void buildDelegateParam( ParameterInfo source, ParameterBuilder destination )
		{
			if( applyCustomMarshalling( source, destination ) )
				return;

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

			if( tParamType.IsArray )
			{
				// Detected array argument, apply [MarshalAs( UnmanagedType.LPArray )]
				object[] ctorArgs = new object[ 1 ] { UnmanagedType.LPArray };
				var cab = new CustomAttributeBuilder( ciMarshalAs, ctorArgs );
				destination.SetCustomAttribute( cab );

				if( dir == eDirection.None )
				{
					// When no direction is specified in the COM interface, default to [In] because it's faster
					cab = new CustomAttributeBuilder( ciInAttribute, emptyObjectArray );
					destination.SetCustomAttribute( cab );
				}
			}

			if( tParamType.isDelegate() )
			{
				Debug.Assert( tParamType.hasCustomAttribute<UnmanagedFunctionPointerAttribute>() );
				object[] ctorArgs = new object[ 1 ] { UnmanagedType.FunctionPtr };
				var cab = new CustomAttributeBuilder( ciMarshalAs, ctorArgs );
				destination.SetCustomAttribute( cab );
			}
		}
	}
}