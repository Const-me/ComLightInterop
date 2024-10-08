﻿using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ComLight.IO
{
	/// <summary>Wraps .NET stream into native iReadStream</summary>
	class NativeReadStream: iReadStream, IDisposable, iComDisposable
	{
		readonly Stream stream;

		NativeReadStream( Stream stream )
		{
			this.stream = stream;
		}

		void iReadStream.getLength( out long length )
		{
			length = stream.Length;
		}

#if !NETCOREAPP
		unsafe
#endif
		void iReadStream.read( ref byte lpBuffer, int nNumberOfBytesToRead, out int lpNumberOfBytesRead )
		{
#if NETCOREAPP
			var span = MemoryMarshal.CreateSpan( ref lpBuffer, nNumberOfBytesToRead );
#else
			var span =  new Span<byte>( Unsafe.AsPointer( ref lpBuffer ), nNumberOfBytesToRead );
#endif
			lpNumberOfBytesRead = stream.Read( span );
		}

		void iReadStream.seek( long offset, eSeekOrigin origin )
		{
			stream.Seek( offset, (SeekOrigin)(byte)origin );
		}

		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose( bool disposing )
		{
			if( !disposedValue )
			{
				if( disposing )
					stream?.Dispose();
				disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose( true );
		}
		void iComDisposable.lastNativeReferenceReleased()
		{
			Dispose( true );
		}

		void iReadStream.getPosition( out long length )
		{
			length = stream.Position;
		}

		static ManagedWrapperCache<Stream, NativeReadStream>.Entry factory( Stream managed, bool addRef )
		{
			NativeReadStream wrapper = new NativeReadStream( managed );
			IntPtr native = ManagedWrapper.wrap<iReadStream>( wrapper, addRef );
			return new ManagedWrapperCache<Stream, NativeReadStream>.Entry( native, wrapper );
		}
		static readonly ManagedWrapperCache<Stream, NativeReadStream> cache = new ManagedWrapperCache<Stream, NativeReadStream>( factory );

		public static IntPtr create( Stream managed, bool addRef )
		{
			return cache.wrap( managed, addRef );
		}
	}
}