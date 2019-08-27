using System;
using System.Reflection.Emit;

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
	}
}