using ComLight;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

static class Tests
{
	public const string dll = "comtest";

	[DllImport( dll, PreserveSig = false )]
	static extern void createTest( [MarshalAs( UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof( Marshaler<ITest> ) )] out ITest obj );

	const int DISP_E_OVERFLOW = unchecked((int)0x8002000A);

	public static void test0()
	{
		ITest test = null;
		createTest( out test );
		test.Dispose();
	}

	public static void test1()
	{
		ITest test = null;
		try
		{
			createTest( out test );
			int r;
			test.add( 1, 2, out r );
			Debug.Assert( r == 3 );
			Console.WriteLine( "Result: {0}", r );

			test.add( int.MinValue, int.MinValue, out r );
		}
		catch( Exception ex )
		{
			Debug.Assert( ex.HResult == DISP_E_OVERFLOW );
			Console.WriteLine( "Exception: {0}", ex.Message );
		}
		test?.Dispose();
	}

	public static void test2()
	{
		ITest test = null;
		createTest( out test );

		ITest managed = new ManagedImpl();
		int r;
		test.addManaged( managed, 1, 2, out r );
		Debug.Assert( r == 3 );
		Console.WriteLine( "Result: {0}", r );
	}

	public static void test3()
	{
		int[] buffer = new int[ 1000000 ];
		var rnd = new Random();
		for( int i = 0; i < buffer.Length; i++ )
		{
			buffer[ i ] = rnd.Next( 0x40000000 );
		}

		ITest test = null;
		createTest( out test );

		int result = 0;
		var sw = Stopwatch.StartNew();
		for( int i = 0; i < buffer.Length; i++ )
		{
			int r;
			test.add( buffer[ i ], buffer[ i ], out r );
			result ^= r;
		}
		sw.Stop();
		TimeSpan e = sw.Elapsed;
		double sec = e.TotalSeconds;
		Console.WriteLine( "{0:X8}", result );

		double ms = sec * 1E+3;
		double nanosecondsPerCall = sec * ( 1E+9 / 1E+6 );
		Console.WriteLine( "Managed->native interop: {0}ms for 1M calls, {1} nanoseconds / call", ms, nanosecondsPerCall );
		// On my PC it says "20 nanoseconds/call", translates to 66 CPU cycles per call @ 3.3GHz stock frequency. Pretty good result, IMO.
	}

	public static void test4()
	{
		ITest test = null;
		createTest( out test );
		ITest managed = new ManagedImpl();
		int res;
		double sec;
		test.testPerformance( managed, out res, out sec );

		double ms = sec * 1E+3;
		double nanosecondsPerCall = sec * ( 1E+9 / 1E+6 );
		Console.WriteLine( "Native->managed interop: {0}ms for 1M calls, {1} nanoseconds / call", ms, nanosecondsPerCall );
	}

	public static void testStream()
	{
		ITest test;
		createTest( out test );

		MemoryStream ms = new MemoryStream();
		using( var w = new StreamWriter( ms, Encoding.ASCII, 1024, true ) )
			w.Write( "Hello, world." );

		MemoryStream ws = new MemoryStream();
		test.testStreams( ms, ws );
		ws.Seek( 0, SeekOrigin.Begin );

		using( var r = new StreamReader( ws, Encoding.ASCII ) )
		{
			string all = r.ReadToEnd();
			Console.WriteLine( all );
		}
	}

	public static void testMarshalBack()
	{
		ITest test;
		createTest( out test );
		ITest managed = new ManagedImpl();
		string path = Path.Combine( Path.GetTempPath(), "test.txt" );
		test.testMarshalBack( path, managed );
	}
}