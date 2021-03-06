﻿using ComLight;
using System;
using System.Runtime.InteropServices;

// Declare an interface, must match to the C++ side of the interop
[ComInterface( "cdc9e3c6-b300-4138-b006-c61e7c2bfe48" )]
public interface IHelloWorld
{
	bool print( string what );
}

class Program
{
	// Import class factory function from *.dll / *.so
	[DllImport( "helloworld", PreserveSig = false )]
	static extern void createHelloWorld( [MarshalAs( UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof( Marshaler<IHelloWorld> ) )] out IHelloWorld obj );

	static void Main( string[] args )
	{
		// Call factory to create the object instance 
		createHelloWorld( out IHelloWorld test );
		// Call a method
		bool res = test.print( "Hello, World." );
		Console.WriteLine( "Returned: {0}", res );
	}
}