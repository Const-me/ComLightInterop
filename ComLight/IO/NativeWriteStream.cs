using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ComLight.IO
{
	/// <summary>Wraps .NET stream into native iWriteStream</summary>
	class NativeWriteStream: iWriteStream, iComDisposable
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

		void iComDisposable.lastNativeReferenceReleased()
		{
			stream?.Dispose();
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

		static ManagedWrapperCache<Stream, NativeWriteStream>.Entry factory( Stream managed, bool addRef )
		{
			NativeWriteStream wrapper = new NativeWriteStream( managed );
			IntPtr native = ManagedWrapper.wrap<iWriteStream>( wrapper, addRef );
			return new ManagedWrapperCache<Stream, NativeWriteStream>.Entry( native, wrapper );
		}
		static readonly ManagedWrapperCache<Stream, NativeWriteStream> cache = new ManagedWrapperCache<Stream, NativeWriteStream>( factory );

		public static IntPtr create( Stream managed, bool addRef )
		{
			return cache.wrap( managed, addRef );
		}
	}
}