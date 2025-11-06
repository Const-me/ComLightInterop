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
public static unsafe class ReadStreamMarshaller
{
	static Stream? toManaged( nint native )
	{
		iReadStream? com = ReadStreamMarshal.NoRef.ConvertToManaged( native );
		if( com == null )
			return null;
		return new ManagedReadStream( native, com );
	}

	static nint toNative( Stream? obj, bool addRef )
	{
		if( obj == null )
			return 0;
		if( obj is ManagedReadStream managed )
			return managed.com;
		NativeReadStream nrs = cache.GetValue( obj, cacheCallback );
		if( addRef )
			return ReadStreamMarshal.AddRef.ConvertToUnmanaged( nrs );
		else
			return ReadStreamMarshal.NoRef.ConvertToUnmanaged( nrs );
	}

	static readonly ConditionalWeakTable<Stream, NativeReadStream> cache = new();
	static readonly ConditionalWeakTable<Stream, NativeReadStream>.CreateValueCallback cacheCallback =
		( Stream stm ) => new NativeReadStream( stm );

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