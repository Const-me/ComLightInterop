namespace ComLight.IO
{
	/// <summary>Specifies the position in a stream to use for seeking.</summary>
	public enum eSeekOrigin: byte
	{
		/// <summary>Specifies the beginning of a stream.</summary>
		Begin = 0,
		/// <summary>Specifies the current position within a stream.</summary>
		Current = 1,
		/// <summary>Specifies the end of a stream.</summary>
		End = 2
	}

	/// <summary>Readonly byte stream interface.</summary>
	[ComInterface( "006af6db-734e-4595-8c94-19304b2389ac" )]
	public interface iReadStream
	{
		/// <summary>Read a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.</summary>
		void read( ref byte lpBuffer, int nNumberOfBytesToRead, out int lpNumberOfBytesRead );
		/// <summary>Set the position within the current stream.</summary>
		void seek( long offset, eSeekOrigin origin );
		/// <summary>Get the position within the current stream.</summary>
		void getPosition( out long length );
		/// <summary>Get the length in bytes of the stream.</summary>
		void getLength( out long length );
	}
}