using System.Runtime.InteropServices;

namespace ComLight.IO
{
	public enum eSeekOrigin: byte
	{
		Begin = 0,
		Current = 1,
		End = 2
	}

	[ComInterface( "006af6db-734e-4595-8c94-19304b2389ac" )]
	public interface iReadStream
	{
		void read( ref byte lpBuffer, int nNumberOfBytesToRead, out int lpNumberOfBytesRead );
		void seek( long offset, eSeekOrigin origin );
		void getPosition( out long length );
		void getLength( out long length );
	}
}