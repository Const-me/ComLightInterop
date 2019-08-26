using ComLight;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DesktopTest
{
	[ComInterface( "a3ccc418-1565-47dc-88ab-78dcfb5cc800" )]
	public interface ITest: IDisposable
	{
		void add( int a, int b, [Out] out int result );
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

		static void Main( string[] args )
		{
			// test0();
			test1();
		}
	}
}
