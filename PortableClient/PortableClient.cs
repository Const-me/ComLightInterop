using ComLight;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PortableClient
{
	static class PortableClient
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

		class ManagedImpl: ITest
		{
			int ITest.add( int a, int b, out int result )
			{
				try
				{
					checked
					{
						result = a + b;
						return 0;
					}
				}
				catch( Exception ex )
				{
					result = 0;
					return ex.HResult;
				}
			}

			void ITest.addManaged( ITest managed, int a, int b, out int result )
			{
				throw new NotImplementedException();
			}

			int ITest.testPerformance( ITest managed, out int xor, out double seconds )
			{
				throw new NotImplementedException();
			}
			void ITest.testStreams( Stream stmRead, Stream stmWrite )
			{
				throw new NotImplementedException();
			}

			void IDisposable.Dispose()
			{
			}
		}

		static void test2()
		{
			ITest test = null;
			createTest( out test );

			ITest managed = new ManagedImpl();
			int r;
			test.addManaged( managed, 1, 2, out r );
			Debug.Assert( r == 3 );
			Console.WriteLine( "Result: {0}", r );
		}

		static void test3()
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

		static void test4()
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

		static void testStream()
		{
			ITest test = null;
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

		public static void runTest()
		{
			// test0();
			// test1();
			// test2();
			// test3();
			// test4();
			testStream();
		}
	}
}