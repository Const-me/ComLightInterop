using System;
using System.IO;

namespace ComLight.IO
{
	/// <summary>Implement .NET write only stream on top of native iWriteStream</summary>
	class ManagedWriteStream: Stream
	{
		internal readonly IntPtr com;
		readonly iWriteStream native;

		internal ManagedWriteStream( IntPtr com, iWriteStream native )
		{
			this.com = com;
			this.native = native;
		}
#if !OFFLINE_CODEGEN
		~ManagedWriteStream()
		{
			cache.dropIfDead( com );
		}
#endif

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
#if NETCOREAPP
			Write( buffer.AsSpan( offset, count ) );
#else
			// var span = new ReadOnlySpan<byte>( buffer, offset, count );
			// Can't use ReadOnlySpan due to API inconsistency, there's no ref readonly arguments, only ref readonly returns

			var span = new Span<byte>( buffer, offset, count );
			native.write( ref span.GetPinnableReference(), count );
#endif
		}

#if NETCOREAPP
		public override void Write( ReadOnlySpan<byte> span )
		{
			unsafe
			{
				fixed( byte* rsi = span )
					native.write( (nint)rsi, span.Length );
			}
		}
#endif

#if !OFFLINE_CODEGEN
		static ManagedWriteStream factory( IntPtr nativeComPointer )
		{
			iWriteStream iws = NativeWrapper.wrap<iWriteStream>( nativeComPointer );
			return new ManagedWriteStream( nativeComPointer, iws );
		}
		static readonly NativeWrapperCache<ManagedWriteStream> cache = new NativeWrapperCache<ManagedWriteStream>( factory );

		/// <summary>Class factory method</summary>
		public static Stream create( IntPtr nativeComPointer )
		{
			return cache.wrap( nativeComPointer );
		}
#endif
	}
}