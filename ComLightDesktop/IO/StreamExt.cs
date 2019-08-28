using System;
using System.Buffers;
using System.IO;

namespace ComLight
{
	public static class StreamExt
	{
		// https://stackoverflow.com/a/53761172/126995
		public static int Read( this Stream thisStream, Span<byte> buffer )
		{
			byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent( buffer.Length );
			try
			{
				int numRead = thisStream.Read( sharedBuffer, 0, buffer.Length );
				if( (uint)numRead > (uint)buffer.Length )
					throw new IOException( "Stream too long" );
				new Span<byte>( sharedBuffer, 0, numRead ).CopyTo( buffer );
				return numRead;
			}
			finally { ArrayPool<byte>.Shared.Return( sharedBuffer ); }
		}

		public static void Write( this Stream thisStream, ReadOnlySpan<byte> buffer )
		{
			byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent( buffer.Length );
			try
			{
				buffer.CopyTo( sharedBuffer );
				thisStream.Write( sharedBuffer, 0, buffer.Length );
			}
			finally { ArrayPool<byte>.Shared.Return( sharedBuffer ); }
		}
	}
}