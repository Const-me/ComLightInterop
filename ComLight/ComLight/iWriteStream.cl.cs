#pragma warning disable CS8981	// The type name only contains lower-cased ascii characters
#pragma warning disable CS8603	// Possible null reference return
#pragma warning disable CS8604	// Possible null reference argument
#pragma warning disable CS8601	// Possible null reference assignment
#nullable enable
namespace ComLight.IO;
using System;
using System.Runtime.InteropServices;
using ComLight;

static class iWriteStream_native
{
	[UnmanagedFunctionPointer( RuntimeClass.defaultCallingConvention )]
	public delegate int write( nint pThis, nint rsi, int nNumberOfBytesToWrite );
	[UnmanagedFunctionPointer( RuntimeClass.defaultCallingConvention )]
	public delegate int flush( nint pThis );
}

sealed class iWriteStream_proxy: RuntimeClass, iWriteStream
{
	internal static iWriteStream_proxy create( nint nativePointer, bool attach ) =>
		new iWriteStream_proxy( nativePointer, readVirtualTable( nativePointer, 2 ), attach );

	readonly iWriteStream_native.write m_write;
	readonly iWriteStream_native.flush m_flush;

	iWriteStream_proxy( nint nativePointer, IntPtr[] vtbl, bool attach ):
		base( nativePointer, vtbl, attach, WriteStreamMarshal.s_iid )
	{
		m_write = Marshal.GetDelegateForFunctionPointer<iWriteStream_native.write>( vtbl[ 3 ] );
		m_flush = Marshal.GetDelegateForFunctionPointer<iWriteStream_native.flush>( vtbl[ 4 ] );
	}

	void iWriteStream.write( nint rsi, int nNumberOfBytesToWrite )
	{
		ErrorCodes.throwForHR( m_write( m_nativePointer, rsi, nNumberOfBytesToWrite ) );
	}

	void iWriteStream.flush()
	{
		ErrorCodes.throwForHR( m_flush( m_nativePointer ) );
	}
}

internal static class WriteStreamMarshal
{
	internal static readonly Guid s_iid = new Guid( "d7c3eb39-9170-43b9-ba98-2ea1f2fed8a8" );

	static Delegate[] managedDelegates( iWriteStream obj )
	{
		iWriteStream_native.write write = delegate( nint _, nint rsi, int nNumberOfBytesToWrite )
		{
			try
			{
				obj.write( rsi, nNumberOfBytesToWrite );
				return 0;
			}
			catch( Exception ex )
			{
				return ex.HResult;
			}
		};
		iWriteStream_native.flush flush = delegate( nint _ )
		{
			try
			{
				obj.flush();
				return 0;
			}
			catch( Exception ex )
			{
				return ex.HResult;
			}
		};
		return [ write, flush ];
	}

	static readonly Func<iWriteStream, Delegate[]> s_factory = managedDelegates;

	internal static iWriteStream? toManaged( nint nativePointer, bool attach )
	{
		if( nativePointer == 0 ) return null;
		return iWriteStream_proxy.create( nativePointer, attach );
	}

	internal static nint toNative( iWriteStream? obj, bool addRef ) =>
		ManagedWrapper.wrapManagedBothWays<iWriteStream>( obj, addRef, s_factory, s_iid );
}