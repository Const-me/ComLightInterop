using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DesktopClient
{
	[InterfaceType( ComInterfaceType.InterfaceIsIUnknown ), Guid( "a3ccc418-1565-47dc-88ab-78dcfb5cc800" )]
	interface ITest
	{
		void add( int a, int b, out int result );
	}

	class Program
	{
		public const string dll = "comtest";

		[DllImport( dll, PreserveSig = false )]
		static extern void createTest( out ITest obj );

		const int DISP_E_OVERFLOW = unchecked((int)0x8002000A);

		static async Task Main( string[] args )
		{
			ITest test = null;
			try
			{
				createTest( out test );
				int r = -1;
				Action act = () => test.add( 1, 2, out r );
				await Task.Run( act );
				Debug.Assert( r == 3 );
				Console.WriteLine( "Result: {0}", r );
				test.add( int.MinValue, int.MinValue, out r );
			}
			catch( Exception ex )
			{
				Debug.Assert( ex.HResult == DISP_E_OVERFLOW );
				Console.WriteLine( "Exception: {0}", ex.Message );
			}
			Marshal.FinalReleaseComObject( test );
		}
	}
}