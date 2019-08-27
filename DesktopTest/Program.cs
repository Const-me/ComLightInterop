using ComLight;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopTest
{
	[ComInterface( "a3ccc418-1565-47dc-88ab-78dcfb5cc800" )]
	public interface ITest: IDisposable
	{
		void add( int a, int b, [Out] out int result );

		// Add 2 numbers by calling ITest.add on the supplied COM interface, testing native to managed marshalling.
		void addManaged( ITest managed, int a, int b, [Out] out int result );

		int testPerformance( ITest managed, out int xor, out double seconds );

		void testReadStream( [ReadStream] Stream stm );
	}

	class Program
	{
		public const string dll = "comtest";

		[DllImport( dll, PreserveSig = false )]
		static extern void createTest( [MarshalAs( UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof( Marshaler<ITest> ) )] out ITest obj );

		const int DISP_E_OVERFLOW = unchecked((int)0x8002000A);

		static void test0()
		{
			ITest test = null;
			createTest( out test );
			test.Dispose();
		}

		static void test1()
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

		static void testStream()
		{
			ITest test = null;
			createTest( out test );

			MemoryStream ms = new MemoryStream();
			using( var w = new StreamWriter( ms, Encoding.ASCII, 1024, true ) )
				w.Write( "Hello, world." );

			test.testReadStream( ms );
		}

		static void Main( string[] args )
		{
			try
			{
				// test0();
				// test1();
				testStream();
			}
			catch( Exception ex )
			{
				Console.WriteLine( ex.ToString() );
			}
		}
	}
}
