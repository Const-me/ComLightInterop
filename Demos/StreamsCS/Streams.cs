using ComLight;
using System.IO;
using System.Runtime.InteropServices;

[ComInterface( "d29d85bf-d6d1-4c4c-8989-ce9260debc60" )]
public interface iFileSystem
{
	void openFile( [NativeString] string path, [ReadStream] out Stream stm );
	void createFile( [NativeString] string path, [WriteStream] out Stream stm );
}

class ManagedFileSystem: iFileSystem
{
	void iFileSystem.createFile( string path, out Stream stm )
	{
		stm = File.Create( path );
	}
	void iFileSystem.openFile( string path, out Stream stm )
	{
		stm = File.OpenRead( path );
	}
}

[ComInterface( "0d30d69c-c9f5-40f1-b16b-77f54de38805" )]
public interface iStreamsDemo
{
	void init( iFileSystem managed, out iFileSystem native );
}

class Program
{
	// Import class factory function from *.dll / *.so
	[DllImport( "streams", PreserveSig = false )]
	static extern void createStreams( [MarshalAs( UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof( Marshaler<iStreamsDemo> ) )] out iStreamsDemo obj );

	static void copyWithNative( iFileSystem nativeFs, string pathFrom, string pathTo )
	{
		Stream from, to;
		nativeFs.openFile( pathFrom, out from );
		using( from )
		{
			nativeFs.createFile( pathTo, out to );
			using( to )
				from.CopyTo( to );
		}
	}

	static void Main( string[] args )
	{
		createStreams( out iStreamsDemo demo );
		iFileSystem managedFs = new ManagedFileSystem();
		demo.init( managedFs, out iFileSystem nativeFs );
		copyWithNative( nativeFs, @"C:\Temp\bases.jpg", @"C:\Temp\bases-copy.jpg" );
	}
}