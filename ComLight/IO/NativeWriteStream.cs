using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ComLight.IO
{
	/// <summary>Wraps .NET stream into native iWriteStream</summary>
	class NativeWriteStream: iWriteStream
	{
		readonly Stream stream;

		NativeWriteStream( Stream stream )
		{
			this.stream = stream;
		}

		void iWriteStream.flush()
		{
			stream.Flush();
		}

#if !NETCOREAPP
		unsafe
#endif
		void iWriteStream.write( ref byte lpBuffer, int nNumberOfBytesToWrite )
		{
#if NETCOREAPP
			var span = MemoryMarshal.CreateReadOnlySpan( ref lpBuffer, nNumberOfBytesToWrite );
#else
			var span =  new ReadOnlySpan<byte>( Unsafe.AsPointer( ref lpBuffer ), nNumberOfBytesToWrite );
#endif
			stream.Write( span );
		}

		static IntPtr factory( Stream managed, bool addRef )
		{
			NativeWriteStream wrapper = new NativeWriteStream( managed );
			return ManagedWrapper.wrap<iWriteStream>( wrapper, addRef );
		}
		static readonly ManagedWrapperCache<Stream> cache = new ManagedWrapperCache<Stream>( factory );

		public static IntPtr create( Stream managed, bool addRef )
		{
			return cache.wrap( managed, addRef );
		}
	}
}