#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using ComLight;

static class iStreamsDemo_native
{
	[UnmanagedFunctionPointer( RuntimeClass.defaultCallingConvention )]
	public delegate int init( nint pThis, nint managed, out nint native );
	[UnmanagedFunctionPointer( RuntimeClass.defaultCallingConvention )]
	public delegate int copyWithManaged( nint pThis, [MarshalAs( UnmanagedType.LPWStr )] string pathFrom, [MarshalAs( UnmanagedType.LPWStr )] string pathTo );
}

sealed class iStreamsDemo_proxy: RuntimeClass, iStreamsDemo
{
	internal static iStreamsDemo_proxy create( nint nativePointer ) =>
		new iStreamsDemo_proxy( nativePointer, readVirtualTable( nativePointer, 2 ) );

	readonly iStreamsDemo_native.init m_init;
	readonly iStreamsDemo_native.copyWithManaged m_copyWithManaged;

	iStreamsDemo_proxy( nint nativePointer, IntPtr[] vtbl ):
		base( nativePointer, vtbl, StreamsDemoMarshal.s_iid )
	{
		m_init = Marshal.GetDelegateForFunctionPointer<iStreamsDemo_native.init>( vtbl[ 3 ] );
		m_copyWithManaged = Marshal.GetDelegateForFunctionPointer<iStreamsDemo_native.copyWithManaged>( vtbl[ 4 ] );
	}

	void iStreamsDemo.init( iFileSystem managed, out iFileSystem native )
	{
		ErrorCodes.throwForHR( m_init( m_nativePointer, FileSystemMarshal.NoRef.ConvertToUnmanaged( managed ), out var _native ) );
		native = FileSystemMarshal.NoRef.ConvertToManaged( _native );
	}

	void iStreamsDemo.copyWithManaged( string pathFrom, string pathTo )
	{
		ErrorCodes.throwForHR( m_copyWithManaged( m_nativePointer, pathFrom, pathTo ) );
	}
}

[CustomMarshaller( typeof(iStreamsDemo), MarshalMode.ManagedToUnmanagedIn, typeof( NoRef ) )]
[CustomMarshaller( typeof(iStreamsDemo), MarshalMode.ManagedToUnmanagedOut, typeof( NoRef ) )]
[CustomMarshaller( typeof(iStreamsDemo), MarshalMode.UnmanagedToManagedIn, typeof( NoRef ) )]
[CustomMarshaller( typeof(iStreamsDemo), MarshalMode.UnmanagedToManagedOut, typeof( AddRef ) )]
[CustomMarshaller( typeof(iStreamsDemo), MarshalMode.ElementIn, typeof( NoRef ) )]
[CustomMarshaller( typeof(iStreamsDemo), MarshalMode.ElementOut, typeof( NoRef ) )]
[CustomMarshaller( typeof(iStreamsDemo), MarshalMode.Default, typeof( Unsup ) )]
internal static unsafe class StreamsDemoMarshal
{
	internal static readonly Guid s_iid = new Guid( "0d30d69c-c9f5-40f1-b16b-77f54de38805" );

	static Delegate[] managedDelegates( iStreamsDemo obj )
	{
		iStreamsDemo_native.init init = delegate( nint _, nint managed, out nint native )
		{
			try
			{
				obj.init( FileSystemMarshal.NoRef.ConvertToManaged( managed ), out var _native );
				native = FileSystemMarshal.AddRef.ConvertToUnmanaged( _native );
				return 0;
			}
			catch( Exception ex )
			{
				native = default;
				return ex.HResult;
			}
		};
		iStreamsDemo_native.copyWithManaged copyWithManaged = delegate( nint _, string pathFrom, string pathTo )
		{
			try
			{
				obj.copyWithManaged( pathFrom, pathTo );
				return 0;
			}
			catch( Exception ex )
			{
				return ex.HResult;
			}
		};
		return [ init, copyWithManaged ];
	}

	static readonly Func<iStreamsDemo, Delegate[]> s_factory = managedDelegates;

	static iStreamsDemo? toManaged( nint nativePointer )
	{
		if( nativePointer == 0 ) return null;
		return iStreamsDemo_proxy.create( nativePointer );
	}

	static nint toNative( iStreamsDemo? obj, bool addRef ) =>
		ManagedWrapper.wrapManagedBothWays<iStreamsDemo>( obj, addRef, s_factory, s_iid );

	public static class NoRef
	{
		public static iStreamsDemo? ConvertToManaged( nint native ) =>
			toManaged( native );
		public static nint ConvertToUnmanaged( iStreamsDemo? obj ) =>
			toNative( obj, false );
		public static void Free( nint native ) { }
	}
	public static class AddRef
	{
		public static iStreamsDemo? ConvertToManaged( nint native ) =>
			toManaged( native );
		public static nint ConvertToUnmanaged( iStreamsDemo? obj ) =>
			toNative( obj, true );
		public static void Free( nint native ) { }
	}
	public static class Unsup
	{
		public static iStreamsDemo? ConvertToManaged( nint native ) =>
			throw new NotSupportedException();
		public static nint ConvertToUnmanaged( iStreamsDemo? obj ) =>
			throw new NotSupportedException();
		public static void Free( nint native ) { }
	}
}

[NativeMarshalling( typeof( StreamsDemoMarshal ) )]
partial interface iStreamsDemo { }