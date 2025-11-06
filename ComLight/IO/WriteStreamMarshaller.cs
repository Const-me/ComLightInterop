#nullable enable
#pragma warning disable CS1591	// Missing XML comments
namespace ComLight.IO;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;

[CustomMarshaller( typeof( Stream ), MarshalMode.ManagedToUnmanagedIn, typeof( NoRef ) )]
[CustomMarshaller( typeof( Stream ), MarshalMode.ManagedToUnmanagedOut, typeof( NoRef ) )]
[CustomMarshaller( typeof( Stream ), MarshalMode.UnmanagedToManagedIn, typeof( NoRef ) )]
[CustomMarshaller( typeof( Stream ), MarshalMode.UnmanagedToManagedOut, typeof( AddRef ) )]
[CustomMarshaller( typeof( Stream ), MarshalMode.Default, typeof( Unsup ) )]
public static unsafe class WriteStreamMarshaller
{
	static Stream? toManaged( nint native )
	{
		iWriteStream? com = WriteStreamMarshal.NoRef.ConvertToManaged( native );
		if( com == null )
			return null;
		return new ManagedWriteStream( native, com );
	}

	static nint toNative( Stream? obj, bool addRef )
	{
		if( obj == null )
			return 0;
		if( obj is ManagedWriteStream managed )
			return managed.com;
		NativeWriteStream nws = cache.GetValue( obj, cacheCallback );
		if( addRef )
			return WriteStreamMarshal.AddRef.ConvertToUnmanaged( nws );
		else
			return WriteStreamMarshal.NoRef.ConvertToUnmanaged( nws );
	}

	static readonly ConditionalWeakTable<Stream, NativeWriteStream> cache = new();
	static readonly ConditionalWeakTable<Stream, NativeWriteStream>.CreateValueCallback cacheCallback =
		( Stream stm ) => new NativeWriteStream( stm );

	public static class NoRef
	{
		public static Stream? ConvertToManaged( nint native ) =>
			toManaged( native );
		public static nint ConvertToUnmanaged( Stream? obj ) =>
			toNative( obj, false );
		public static void Free( nint native ) { }
	}
	public static class AddRef
	{
		public static Stream? ConvertToManaged( nint native ) =>
			toManaged( native );
		public static nint ConvertToUnmanaged( Stream? obj ) =>
			toNative( obj, true );
		public static void Free( nint native ) { }
	}
	public static class Unsup
	{
		public static Stream? ConvertToManaged( nint native ) =>
			throw new NotSupportedException();
		public static nint ConvertToUnmanaged( Stream? obj ) =>
			throw new NotSupportedException();
		public static void Free( nint native ) { }
	}
}