#pragma warning disable CS8981	// The type name only contains lower-cased ascii characters
#pragma warning disable CS8603	// Possible null reference return
#pragma warning disable CS8604	// Possible null reference argument
#pragma warning disable CS8601	// Possible null reference assignment
#nullable enable
using System;
using System.Runtime.InteropServices;
using ComLight;

static class iFileSystem_native
{
	[UnmanagedFunctionPointer( RuntimeClass.defaultCallingConvention )]
	public delegate int openFile( nint pThis, [MarshalAs( UnmanagedType.LPWStr )] string path, out nint stm );
	[UnmanagedFunctionPointer( RuntimeClass.defaultCallingConvention )]
	public delegate int createFile( nint pThis, [MarshalAs( UnmanagedType.LPWStr )] string path, out nint stm );
}

sealed class iFileSystem_proxy: RuntimeClass, iFileSystem
{
	internal static iFileSystem_proxy create( nint nativePointer, bool attach ) =>
		new iFileSystem_proxy( nativePointer, readVirtualTable( nativePointer, 2 ), attach );

	readonly iFileSystem_native.openFile m_openFile;
	readonly iFileSystem_native.createFile m_createFile;

	iFileSystem_proxy( nint nativePointer, IntPtr[] vtbl, bool attach ):
		base( nativePointer, vtbl, attach, FileSystemMarshal.s_iid )
	{
		m_openFile = Marshal.GetDelegateForFunctionPointer<iFileSystem_native.openFile>( vtbl[ 3 ] );
		m_createFile = Marshal.GetDelegateForFunctionPointer<iFileSystem_native.createFile>( vtbl[ 4 ] );
	}

	void iFileSystem.openFile( string path, out System.IO.Stream stm )
	{
		ErrorCodes.throwForHR( m_openFile( m_nativePointer, path, out var _stm ) );
		stm = ComLight.IO.ReadStreamMarshaller.toManaged( _stm, true );
	}

	void iFileSystem.createFile( string path, out System.IO.Stream stm )
	{
		ErrorCodes.throwForHR( m_createFile( m_nativePointer, path, out var _stm ) );
		stm = ComLight.IO.WriteStreamMarshaller.toManaged( _stm, true );
	}
}

internal static class FileSystemMarshal
{
	internal static readonly Guid s_iid = new Guid( "d29d85bf-d6d1-4c4c-8989-ce9260debc60" );

	static Delegate[] managedDelegates( iFileSystem obj )
	{
		iFileSystem_native.openFile openFile = delegate( nint _, string path, out nint stm )
		{
			try
			{
				obj.openFile( path, out var _stm );
				stm = ComLight.IO.ReadStreamMarshaller.toNative( _stm, true );
				return 0;
			}
			catch( Exception ex )
			{
				stm = default;
				return ex.HResult;
			}
		};
		iFileSystem_native.createFile createFile = delegate( nint _, string path, out nint stm )
		{
			try
			{
				obj.createFile( path, out var _stm );
				stm = ComLight.IO.WriteStreamMarshaller.toNative( _stm, true );
				return 0;
			}
			catch( Exception ex )
			{
				stm = default;
				return ex.HResult;
			}
		};
		return [ openFile, createFile ];
	}

	static readonly Func<iFileSystem, Delegate[]> s_factory = managedDelegates;

	internal static iFileSystem? toManaged( nint nativePointer, bool attach )
	{
		if( nativePointer == 0 ) return null;
		return iFileSystem_proxy.create( nativePointer, attach );
	}

	internal static nint toNative( iFileSystem? obj, bool addRef ) =>
		ManagedWrapper.wrapManagedBothWays<iFileSystem>( obj, addRef, s_factory, s_iid );
}