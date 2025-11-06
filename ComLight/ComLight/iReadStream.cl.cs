#pragma warning disable CS8981	// The type name only contains lower-cased ascii characters
#pragma warning disable CS8603	// Possible null reference return
#pragma warning disable CS8604	// Possible null reference argument
#pragma warning disable CS8601	// Possible null reference assignment
#nullable enable
namespace ComLight.IO;
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using ComLight;

static class iReadStream_native
{
	[UnmanagedFunctionPointer( RuntimeClass.defaultCallingConvention )]
	public delegate int read( nint pThis, ref byte lpBuffer, int nNumberOfBytesToRead, out int lpNumberOfBytesRead );
	[UnmanagedFunctionPointer( RuntimeClass.defaultCallingConvention )]
	public delegate int seek( nint pThis, long offset, ComLight.IO.eSeekOrigin origin );
	[UnmanagedFunctionPointer( RuntimeClass.defaultCallingConvention )]
	public delegate int getPosition( nint pThis, out long length );
	[UnmanagedFunctionPointer( RuntimeClass.defaultCallingConvention )]
	public delegate int getLength( nint pThis, out long length );
}

sealed class iReadStream_proxy: RuntimeClass, iReadStream
{
	internal static iReadStream_proxy create( nint nativePointer, bool attach ) =>
		new iReadStream_proxy( nativePointer, readVirtualTable( nativePointer, 4 ), attach );

	readonly iReadStream_native.read m_read;
	readonly iReadStream_native.seek m_seek;
	readonly iReadStream_native.getPosition m_getPosition;
	readonly iReadStream_native.getLength m_getLength;

	iReadStream_proxy( nint nativePointer, IntPtr[] vtbl, bool attach ):
		base( nativePointer, vtbl, attach, ReadStreamMarshal.s_iid )
	{
		m_read = Marshal.GetDelegateForFunctionPointer<iReadStream_native.read>( vtbl[ 3 ] );
		m_seek = Marshal.GetDelegateForFunctionPointer<iReadStream_native.seek>( vtbl[ 4 ] );
		m_getPosition = Marshal.GetDelegateForFunctionPointer<iReadStream_native.getPosition>( vtbl[ 5 ] );
		m_getLength = Marshal.GetDelegateForFunctionPointer<iReadStream_native.getLength>( vtbl[ 6 ] );
	}

	void iReadStream.read( ref byte lpBuffer, int nNumberOfBytesToRead, out int lpNumberOfBytesRead )
	{
		ErrorCodes.throwForHR( m_read( m_nativePointer, ref lpBuffer, nNumberOfBytesToRead, out lpNumberOfBytesRead ) );
	}

	void iReadStream.seek( long offset, ComLight.IO.eSeekOrigin origin )
	{
		ErrorCodes.throwForHR( m_seek( m_nativePointer, offset, origin ) );
	}

	void iReadStream.getPosition( out long length )
	{
		ErrorCodes.throwForHR( m_getPosition( m_nativePointer, out length ) );
	}

	void iReadStream.getLength( out long length )
	{
		ErrorCodes.throwForHR( m_getLength( m_nativePointer, out length ) );
	}
}

[CustomMarshaller( typeof(iReadStream), MarshalMode.ManagedToUnmanagedIn, typeof( NoRef ) )]
[CustomMarshaller( typeof(iReadStream), MarshalMode.ManagedToUnmanagedOut, typeof( AddRef ) )]
[CustomMarshaller( typeof(iReadStream), MarshalMode.UnmanagedToManagedIn, typeof( NoRef ) )]
[CustomMarshaller( typeof(iReadStream), MarshalMode.UnmanagedToManagedOut, typeof( AddRef ) )]
[CustomMarshaller( typeof(iReadStream), MarshalMode.Default, typeof( Unsup ) )]
internal static unsafe class ReadStreamMarshal
{
	internal static readonly Guid s_iid = new Guid( "006af6db-734e-4595-8c94-19304b2389ac" );

	static Delegate[] managedDelegates( iReadStream obj )
	{
		iReadStream_native.read read = delegate( nint _, ref byte lpBuffer, int nNumberOfBytesToRead, out int lpNumberOfBytesRead )
		{
			try
			{
				obj.read( ref lpBuffer, nNumberOfBytesToRead, out lpNumberOfBytesRead );
				return 0;
			}
			catch( Exception ex )
			{
				lpNumberOfBytesRead = default;
				return ex.HResult;
			}
		};
		iReadStream_native.seek seek = delegate( nint _, long offset, ComLight.IO.eSeekOrigin origin )
		{
			try
			{
				obj.seek( offset, origin );
				return 0;
			}
			catch( Exception ex )
			{
				return ex.HResult;
			}
		};
		iReadStream_native.getPosition getPosition = delegate( nint _, out long length )
		{
			try
			{
				obj.getPosition( out length );
				return 0;
			}
			catch( Exception ex )
			{
				length = default;
				return ex.HResult;
			}
		};
		iReadStream_native.getLength getLength = delegate( nint _, out long length )
		{
			try
			{
				obj.getLength( out length );
				return 0;
			}
			catch( Exception ex )
			{
				length = default;
				return ex.HResult;
			}
		};
		return [ read, seek, getPosition, getLength ];
	}

	static readonly Func<iReadStream, Delegate[]> s_factory = managedDelegates;

	static iReadStream? toManaged( nint nativePointer, bool attach )
	{
		if( nativePointer == 0 ) return null;
		return iReadStream_proxy.create( nativePointer, attach );
	}

	static nint toNative( iReadStream? obj, bool addRef ) =>
		ManagedWrapper.wrapManagedBothWays<iReadStream>( obj, addRef, s_factory, s_iid );

	public static class NoRef
	{
		public static iReadStream? ConvertToManaged( nint native ) =>
			toManaged( native, false );
		public static nint ConvertToUnmanaged( iReadStream? obj ) =>
			toNative( obj, false );
		public static void Free( nint native ) { }
	}
	public static class AddRef
	{
		public static iReadStream? ConvertToManaged( nint native ) =>
			toManaged( native, true );
		public static nint ConvertToUnmanaged( iReadStream? obj ) =>
			toNative( obj, true );
		public static void Free( nint native ) { }
	}
	public static class Unsup
	{
		public static iReadStream? ConvertToManaged( nint native ) =>
			throw new NotSupportedException();
		public static nint ConvertToUnmanaged( iReadStream? obj ) =>
			throw new NotSupportedException();
		public static void Free( nint native ) { }
	}
}

[NativeMarshalling( typeof( ReadStreamMarshal ) )]
partial interface iReadStream { }