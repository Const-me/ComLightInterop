using System;
using System.IO;

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

		void iWriteStream.write( byte[] buffer, int count )
		{
			stream.Write( buffer, 0, count );
		}

		static IntPtr factory( Stream managed )
		{
			NativeWriteStream wrapper = new NativeWriteStream( managed );
			return ManagedWrapper.wrap<iWriteStream>( wrapper );
		}
		static readonly ManagedWrapperCache<Stream> cache = new ManagedWrapperCache<Stream>( factory );

		public static IntPtr create( Stream managed )
		{
			return cache.wrap( managed );
		}
	}
}