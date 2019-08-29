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

	int testPerformance( ITest managed, out int xor, out double seconds );

	void testStreams( [ReadStream] Stream stmRead, [WriteStream] Stream stmWrite );

	void createFile( [NativeString] string str, [WriteStream] out Stream stmWrite );
}