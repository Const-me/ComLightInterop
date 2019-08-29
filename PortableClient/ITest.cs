using ComLight;
using System;
using System.IO;
using System.Runtime.InteropServices;

[ComInterface( "a3ccc418-1565-47dc-88ab-78dcfb5cc800" )]
public interface ITest: IDisposable
{
	// Just add 2 numbers
	int add( int a, int b, [Out] out int result );

	// Add 2 numbers by calling ITest.add on the supplied COM interface, testing native to managed marshaling.
	void addManaged( ITest managed, int a, int b, [Out] out int result );

	// Add 1M random numbers in native code, by calling the ITest.add on the supplied interface.
	void testPerformance( ITest managed, out int xor, out double seconds );

	// Does the same as Stream.CopyTo, implemented across the interop boundary.
	void testStreams( [ReadStream] Stream stmRead, [WriteStream] Stream stmWrite );

	// Create a file for writing, return the stream
	void createFile( [NativeString] string str, [WriteStream] out Stream stmWrite );

	// Create a file for writing by calling ITest.createFile on the supplied interface, write hello world there.
	void testMarshalBack( [NativeString] string str, ITest managed );
}