using ComLight;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PortableClient
{
	[ComInterface( "a3ccc418-1565-47dc-88ab-78dcfb5cc800" )]
	public interface ITest: IDisposable
	{
		// Just add 2 numbers
		int add( int a, int b, [Out] out int result );

		// Add 2 numbers by calling ITest.add on the supplied COM interface, testing native to managed marshalling.
		void addManaged( ITest managed, int a, int b, [Out] out int result );

		int testPerformance( ITest managed, out int xor, out double seconds );

		void testReadStream( [ReadStream] Stream stm );
	}

	class Program
	{
		static void Main( string[] args )
		{
			if( Environment.OSVersion.Platform == PlatformID.Unix )
			{
				// Workaround the DLL search path bug on Linux:
				// https://github.com/dotnet/coreclr/issues/18599
				LinuxUtils.preloadDll( "libcomtest.so" );
			}

			// Console.WriteLine( "Hello, world" );
			Console.WriteLine( "64 bit process: {0}", Environment.Is64BitProcess );

			PortableClient.runTest();
		}
	}
}