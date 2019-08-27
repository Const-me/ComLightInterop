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
		int read( [Out, MarshalAs( UnmanagedType.LPArray, SizeParamIndex = 1 )] byte[] buffer, int count );
		int seek( long offset, eSeekOrigin origin );
		int getPosition( out long length );
		int getLength( out long length );
	}
}