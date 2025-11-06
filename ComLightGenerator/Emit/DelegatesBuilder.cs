namespace ComLightGenerator.Emit;
using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Runtime.InteropServices;

sealed class DelegatesBuilder: IDisposable
{
	readonly StreamWriter w;

	public DelegatesBuilder( StreamWriter w, string name )
	{
		this.w = w;
		w.WriteLine( "static class {0}", name );
		w.WriteLine( "{" );
	}

	public void Dispose()
	{
		w.WriteLine( "}" );
	}

	public void addDelegate( ComMethod mi )
	{
		w.WriteLine( "	[UnmanagedFunctionPointer( RuntimeClass.defaultCallingConvention )]" );
		w.Write( "	public delegate " );
		byte? rvi = mi.retValIndex;
		if( rvi.HasValue || !mi.method.ReturnType.isIntPtr() )
			w.Write( "int " );
		else
			w.Write( "nint " );
		w.Write( mi.name );
		w.Write( "( nint pThis" );
		var arr = mi.parameters;
		int retValIndex = -1;
		if( rvi != null )
			retValIndex = rvi.Value;
		int j = 1;
		for( int i = 0; i < arr.Length; i++, j++ )
		{
			if( i == retValIndex )
			{
				retValIndex = -1;
				string argType = nativeRetValArgType( mi );
				w.Write( ", {0} _RetVal", argType );
				i--;
				continue;
			}
			w.Write( ", " );
			applyAttributes( arr[ i ], j - i );
			w.Write( "{0} {1}", arr[ i ].nativeArgType(), arr[ i ].name );
		}
		if( retValIndex >= 0 )
		{
			string argType = nativeRetValArgType( mi );
			w.Write( ", {0} _RetVal", argType );
		}
		w.WriteLine( " );" );
	}

	void applyAttributes( in ComParameter ps, int indexOffset )
	{
		if( ps.isComInterface )
			return;

		bool marshalled = applyMarshalAs( ps, indexOffset );

		if( !marshalled )
		{
			if( ps.isBool )
			{
				// C++ booleans are 1 byte each
				w.Write( "[MarshalAs( UnmanagedType.U1 )] " );
			}
			else if( ps.isArray )
			{
				// Marshal arrays as UnmanagedType.LPArray
				w.Write( "[MarshalAs( UnmanagedType.LPArray )] " );
			}
		}

		if( ps.emitInAttrtibute() )
			w.Write( "[In] " );
	}

	bool applyMarshalAs( in ComParameter ps, int indexOffset )
	{
		AttributeData? marshal = ps.marshalAs;
		if( marshal == null )
			return false;
		// Constructor args
		UnmanagedType ut = (UnmanagedType)(int)marshal.ConstructorArguments[ 0 ].Value!;
		string[] ctorArgs = [ $"UnmanagedType.{ut}" ];

		// Named args
		var namedArgs = marshal.NamedArguments
			.Select( na =>
			{
				if( na.Key == "SizeParamIndex" && na.Value.Value is int v )
					return $"{na.Key} = {v + indexOffset}";
				return $"{na.Key} = {na.Value.Value}";
			} );
		var bothArgs = ctorArgs.Concat( namedArgs ).ToList();
		string argsString = string.Join( ", ", bothArgs );
		w.Write( "[MarshalAs( {0} )] ", argsString );
		return true;
	}

	static bool isMarshalAs( AttributeData a ) =>
		a.AttributeClass?.str() == AttributeNames.marshalAs;

	internal static string nativeRetValArgType( ComMethod mi )
	{
		switch( mi.returns )
		{
			case eMethodReturn.Value:
				return $"out {mi.method.ReturnType.str()}";
			case eMethodReturn.Object:
				return "out nint";
			default:
				throw new ArgumentException();
		}
	}
}