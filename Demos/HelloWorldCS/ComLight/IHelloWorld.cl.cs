#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using ComLight;

static class IHelloWorld_native
{
	[UnmanagedFunctionPointer( RuntimeClass.defaultCallingConvention )]
	public delegate int print( nint pThis, [MarshalAs( UnmanagedType.LPUTF8Str )] string what );
}

sealed class IHelloWorld_proxy: RuntimeClass, IHelloWorld
{
	internal static IHelloWorld_proxy create( nint nativePointer ) =>
		new IHelloWorld_proxy( nativePointer, readVirtualTable( nativePointer, 1 ) );

	readonly IHelloWorld_native.print m_print;

	IHelloWorld_proxy( nint nativePointer, IntPtr[] vtbl ):
		base( nativePointer, vtbl, HelloWorldMarshal.s_iid )
	{
		m_print = Marshal.GetDelegateForFunctionPointer<IHelloWorld_native.print>( vtbl[ 3 ] );
	}

	bool IHelloWorld.print( string what )
	{
		return ErrorCodes.throwAndReturnBool( m_print( m_nativePointer, what ) );
	}
}

[CustomMarshaller( typeof(IHelloWorld), MarshalMode.ManagedToUnmanagedIn, typeof( NoRef ) )]
[CustomMarshaller( typeof(IHelloWorld), MarshalMode.ManagedToUnmanagedOut, typeof( NoRef ) )]
[CustomMarshaller( typeof(IHelloWorld), MarshalMode.UnmanagedToManagedIn, typeof( NoRef ) )]
[CustomMarshaller( typeof(IHelloWorld), MarshalMode.UnmanagedToManagedOut, typeof( AddRef ) )]
[CustomMarshaller( typeof(IHelloWorld), MarshalMode.ElementIn, typeof( NoRef ) )]
[CustomMarshaller( typeof(IHelloWorld), MarshalMode.ElementOut, typeof( NoRef ) )]
[CustomMarshaller( typeof(IHelloWorld), MarshalMode.Default, typeof( Unsup ) )]
internal static unsafe class HelloWorldMarshal
{
	internal static readonly Guid s_iid = new Guid( "cdc9e3c6-b300-4138-b006-c61e7c2bfe48" );

	static Delegate[] managedDelegates( IHelloWorld obj )
	{
		IHelloWorld_native.print print = delegate( nint _, string what )
		{
			try
			{
				return obj.print( what ) ? 0 : 1;
			}
			catch( Exception ex )
			{
				return ex.HResult;
			}
		};
		return [ print ];
	}

	static readonly Func<IHelloWorld, Delegate[]> s_factory = managedDelegates;

	static IHelloWorld? toManaged( nint nativePointer )
	{
		if( nativePointer == 0 ) return null;
		return IHelloWorld_proxy.create( nativePointer );
	}

	static nint toNative( IHelloWorld? obj, bool addRef ) =>
		ManagedWrapper.wrapManagedBothWays<IHelloWorld>( obj, addRef, s_factory, s_iid );

	public static class NoRef
	{
		public static IHelloWorld? ConvertToManaged( nint native ) =>
			toManaged( native );
		public static nint ConvertToUnmanaged( IHelloWorld? obj ) =>
			toNative( obj, false );
		public static void Free( nint native ) { }
	}
	public static class AddRef
	{
		public static IHelloWorld? ConvertToManaged( nint native ) =>
			toManaged( native );
		public static nint ConvertToUnmanaged( IHelloWorld? obj ) =>
			toNative( obj, true );
		public static void Free( nint native ) { }
	}
	public static class Unsup
	{
		public static IHelloWorld? ConvertToManaged( nint native ) =>
			throw new NotSupportedException();
		public static nint ConvertToUnmanaged( IHelloWorld? obj ) =>
			throw new NotSupportedException();
		public static void Free( nint native ) { }
	}
}

[NativeMarshalling( typeof( HelloWorldMarshal ) )]
partial interface IHelloWorld { }