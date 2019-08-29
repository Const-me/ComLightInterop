using System;

namespace PortableClient
{
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

			Tests.testMarshalBack();
		}
	}
}