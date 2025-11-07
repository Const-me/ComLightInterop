#nullable enable
#pragma warning disable CS1591	// Missing XML comments
namespace ComLight.IO;
using System.IO;
using System.Runtime.CompilerServices;

public static class WriteStreamMarshaller
{
	public static Stream? toManaged( nint native, bool attach )
	{
		iWriteStream? com = WriteStreamMarshal.toManaged( native, attach );
		if( com == null )
			return null;
		return new ManagedWriteStream( native, com );
	}

	public static nint toNative( Stream? obj, bool addRef )
	{
		if( obj == null )
			return 0;
		if( obj is ManagedWriteStream managed )
			return managed.com;
		NativeWriteStream nws = cache.GetValue( obj, cacheCallback );
		return WriteStreamMarshal.toNative( nws, addRef );
	}

	static readonly ConditionalWeakTable<Stream, NativeWriteStream> cache = new();
	static readonly ConditionalWeakTable<Stream, NativeWriteStream>.CreateValueCallback cacheCallback =
		( Stream stm ) => new NativeWriteStream( stm );
}