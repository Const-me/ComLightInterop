using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PortableClient
{
	static class LinuxUtils
	{
		private const int RTLD_LAZY = 0x00001;	//Only resolve symbols as needed
		private const int RTLD_GLOBAL = 0x00100;	//Make symbols available to libraries loaded later

		[DllImport( "dl" )]
		static extern IntPtr dlopen( string file, int mode );

		public static bool preloadDll( string nameDll )
		{
			var exe = Assembly.GetExecutingAssembly().Location;
			string dir = Path.GetDirectoryName( exe );
			string pathDll = Path.Combine( dir, nameDll );
			if( !File.Exists( pathDll ) )
				return false;
			IntPtr res = dlopen( pathDll, RTLD_LAZY | RTLD_GLOBAL );
			if( res != IntPtr.Zero )
				Console.WriteLine( "Preloaded the DLL from \"{0}\"", pathDll );
			else
				Console.WriteLine( "dlopen failed to load the DLL from \"{0}\"", pathDll );
			return true;
		}
	}
}