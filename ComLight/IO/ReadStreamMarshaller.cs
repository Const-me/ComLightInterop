#nullable enable
#pragma warning disable CS1591	// Missing XML comments
namespace ComLight.IO;
using System.IO;
using System.Runtime.CompilerServices;

public static class ReadStreamMarshaller
{
	public static Stream? toManaged( nint native, bool attach )
	{
		iReadStream? com = ReadStreamMarshal.toManaged( native, attach );
		if( com == null )
			return null;
		return new ManagedReadStream( native, com );
	}

	public static nint toNative( Stream? obj, bool addRef )
	{
		if( obj == null )
			return 0;
		if( obj is ManagedReadStream managed )
			return managed.com;
		NativeReadStream nrs = cache.GetValue( obj, cacheCallback );
		return ReadStreamMarshal.toNative( nrs, addRef );
	}

	static readonly ConditionalWeakTable<Stream, NativeReadStream> cache = new();
	static readonly ConditionalWeakTable<Stream, NativeReadStream>.CreateValueCallback cacheCallback =
		( Stream stm ) => new NativeReadStream( stm );
}