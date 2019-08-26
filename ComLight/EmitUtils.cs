using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace ComLight
{
	static class EmitUtils
	{
		static readonly OpCode[] intConstants = new OpCode[ 9 ]
		{
			OpCodes.Ldc_I4_0, OpCodes.Ldc_I4_1, OpCodes.Ldc_I4_2, OpCodes.Ldc_I4_3, OpCodes.Ldc_I4_4, OpCodes.Ldc_I4_5, OpCodes.Ldc_I4_6, OpCodes.Ldc_I4_7, OpCodes.Ldc_I4_8
		};

		/// <summary>Push a constant int32 on the stack</summary>
		public static void pushIntConstant( this ILGenerator il, int i )
		{
			if( i >= 0 && i <= 8 )
				il.Emit( intConstants[ i ] );
			else if( i >= -128 && i <= 127 )
				il.Emit( OpCodes.Ldc_I4_S, (sbyte)i );
			else
				il.Emit( OpCodes.Ldc_I4, i );
		}

		static readonly OpCode[] ldArgOpcodes = new OpCode[ 4 ]
		{
			OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Ldarg_2, OpCodes.Ldarg_3
		};

		/// <summary>Load argument to the stack, by index</summary>
		public static void loadArg( this ILGenerator il, int idx )
		{
			if( idx < 0 || idx >= 0x10000 )
				throw new ArgumentOutOfRangeException();
			if( idx < 4 )
				il.Emit( ldArgOpcodes[ idx ] );
			else if( idx < 0x100 )
				il.Emit( OpCodes.Ldarg_S, (byte)idx );
			else
			{
				ushort us = (ushort)idx;
				short ss = unchecked((short)us);
				il.Emit( OpCodes.Ldarg, ss );
			}
		}

		static readonly ConstructorInfo ciMarshalAs;
		static readonly FieldInfo fiMarshalTypeRef;

		static EmitUtils()
		{
			Type tp = typeof( MarshalAsAttribute );
			ciMarshalAs = tp.GetConstructor( new Type[ 1 ] { typeof( UnmanagedType ) } );
			fiMarshalTypeRef = tp.GetField( "MarshalTypeRef" );
		}

		public static void copyCustomAttributes( ParameterInfo source, ParameterBuilder destination )
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