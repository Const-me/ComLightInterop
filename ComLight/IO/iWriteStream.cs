using System.Runtime.InteropServices;

namespace ComLight
{
	/// <summary>Write only byte stream interface.</summary>
	[ComInterface( "d7c3eb39-9170-43b9-ba98-2ea1f2fed8a8" )]
	public interface iWriteStream
	{
		/// <summary>write a sequence of bytes to the current stream and advance the current position within this stream by the number of bytes written.</summary>
		void write( [In] ref byte lpBuffer, int nNumberOfBytesToWrite );
		/// <summary>Clear all buffers for this stream and causes any buffered data to be written to the underlying device.</summary>
		void flush();
	}
}