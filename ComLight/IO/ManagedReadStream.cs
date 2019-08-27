using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace ComLight.IO
{
	/// <summary>Implement .NET readonly stream on top of native iReadStream</summary>
	class ManagedReadStream: Stream
	{
		readonly iReadStream native;

		ManagedReadStream( iReadStream native )
		{
			this.native = native;
		}

		public override bool CanRead => true;

		public override bool CanSeek => true;

		public override bool CanWrite => false;

		public override long Length
		{
			get
			{
				long res;
				Marshal.ThrowExceptionForHR( native.getLength( out res ) );
				return res;
			}
		}

		public override long Position
		{
			get
			{
				long pos;
				Marshal.ThrowExceptionForHR( native.getPosition( out pos ) );
				return pos;
			}
			set
			{
				Seek( value, SeekOrigin.Begin );
			}
		}

		public override void Flush()
		{
			throw new NotSupportedException();
		}

		public override int Read( byte[] buffer, int offset, int count )
		{
			int hr = native.read( buffer, buffer.Length, offset, count );
			Marshal.ThrowExceptionForHR( hr );
			return count;
		}

		public override long Seek( long offset, SeekOrigin origin )
		{
			eSeekOrigin so = (eSeekOrigin)(byte)origin;
			Marshal.ThrowExceptionForHR( native.seek( offset, so ) );
			return Position;
		}

		public override void SetLength( long value )
		{
			throw new NotSupportedException();
		}

		public override void Write( byte[] buffer, int offset, int count )
		{
			throw new NotSupportedException();
		}

		static ManagedReadStream factory( IntPtr nativeComPointer )
		{
			iReadStream irs = NativeWrapper.wrap<iReadStream>( nativeComPointer );
			return new ManagedReadStream( irs );
		}
		static readonly NativeWrapperCache<ManagedReadStream> cache = new NativeWrapperCache<ManagedReadStream>( factory );

		/// <summary>Class factory method</summary>
		public static Stream create( IntPtr nativeComPointer )
		{
			return cache.wrap( nativeComPointer );
		}
	}
}