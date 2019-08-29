using System.Runtime.InteropServices;

namespace ComLight
{
	[ComInterface( "d7c3eb39-9170-43b9-ba98-2ea1f2fed8a8" )]
	public interface iWriteStream
	{
		void write( [In] ref byte lpBuffer, int nNumberOfBytesToWrite );
		void flush();
	}
}