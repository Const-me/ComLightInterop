using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace ComLight.IO
{
	/// <summary>Wraps .NET stream into native iReadStream</summary>
	class NativeReadStream: iReadStream, IDisposable
	{
		readonly Stream stream;

		NativeReadStream( Stream stream )
		{
			this.stream = stream;
		}

		int iReadStream.getLength( out long length )
		{
			length = stream.Length;
			return 0;
		}

		const int S_OK = 0;
		const int S_FALSE = 1;
		// TODO
		const int E_EOF = -1;

		int iReadStream.read( byte[] buffer, int count )
		{
			if( count <= 0 )
				return S_FALSE;

			int offset = 0;
			while( true )
			{
				int cb = stream.Read( buffer, offset, count );
				if( cb <= 0 )
					return E_EOF;
				offset += cb;
				count -= cb;
				if( count <= 0 )
					return S_OK;
			}
		}

		int iReadStream.seek( long offset, eSeekOrigin origin )
		{
			stream.Seek( offset, (SeekOrigin)(byte)origin );
			return S_OK;
		}

		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose( bool disposing )
		{
			if( !disposedValue )
			{
				if( disposing )
				{
					stream?.Dispose();
				}
				disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose( true );
		}

		int iReadStream.getPosition( out long length )
		{
			length = stream.Position;
			return 0;
		}

		static IntPtr factory( Stream managed )
		{
			NativeReadStream wrapper = new NativeReadStream( managed );
			return ManagedWrapper.wrap<iReadStream>( wrapper );
		}
		static readonly ManagedWrapperCache<Stream> cache = new ManagedWrapperCache<Stream>( factory );

		public static IntPtr create( Stream managed )
		{
			return cache.wrap( managed );
		}
	}
}