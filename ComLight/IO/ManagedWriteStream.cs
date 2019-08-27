using System;
using System.IO;

namespace ComLight.IO
{
	/// <summary>Implement .NET write only stream on top of native iWriteStream</summary>
	class ManagedWriteStream: Stream
	{
		readonly iWriteStream native;

		ManagedWriteStream( iWriteStream native )
		{
			this.native = native;
		}

		public override bool CanRead => false;

		public override bool CanSeek => false;

		public override bool CanWrite => true;

		public override long Length => throw new NotSupportedException();

		public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

		public override void Flush()
		{
			native.flush();
		}

		public override int Read( byte[] buffer, int offset, int count )
		{
			throw new NotSupportedException();
		}

		public override long Seek( long offset, SeekOrigin origin )
		{
			throw new NotSupportedException();
		}

		public override void SetLength( long value )
		{
			throw new NotSupportedException();
		}

		public override void Write( byte[] buffer, int offset, int count )
		{
			if( offset == 0 )
				native.write( buffer, count );
			else
			{
				byte[] smallerBuffer = new byte[ count ];
				Buffer.BlockCopy( buffer, offset, smallerBuffer, offset, count );
				native.write( smallerBuffer, count );
			}
		}

		static ManagedWriteStream factory( IntPtr nativeComPointer )
		{
			iWriteStream irs = NativeWrapper.wrap<iWriteStream>( nativeComPointer );
			return new ManagedWriteStream( irs );
		}
		static readonly NativeWrapperCache<ManagedWriteStream> cache = new NativeWrapperCache<ManagedWriteStream>( factory );

		/// <summary>Class factory method</summary>
		public static Stream create( IntPtr nativeComPointer )
		{
			return cache.wrap( nativeComPointer );
		}
	}
}