using ComLight;
using System;
using System.Runtime.InteropServices;

// Declare an interface, must match to the C++ side of the interop
[ComInterface( "cdc9e3c6-b300-4138-b006-c61e7c2bfe48" )]
public partial interface IHelloWorld
{
	bool print( [MarshalAs( UnmanagedType.LPUTF8Str )] string what );
}

partial class Program
{
	[LibraryImport( "helloworld" )]
	internal static partial int createHelloWorld( out IHelloWorld obj );

	static void Main( string[] args )
	{
		// Call factory to create the object instance 
		createHelloWorld( out IHelloWorld test );
		// throw new NotImplementedException();
		// Call a method
		bool res = test.print( "Hello, World." );
		Console.WriteLine( "Returned: {0}", res );
	}
}