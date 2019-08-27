using System.Runtime.InteropServices;

namespace ComLight
{
	[ComInterface( "d7c3eb39-9170-43b9-ba98-2ea1f2fed8a8" )]
	public interface iWriteStream
	{
		void write( [In, MarshalAs( UnmanagedType.LPArray, SizeParamIndex = 1 )] byte[] buffer, int count );
		void flush();
	}
}