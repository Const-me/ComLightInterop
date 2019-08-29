using System;
using System.IO;

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

	void ITest.testPerformance( ITest managed, out int xor, out double seconds )
	{
		throw new NotImplementedException();
	}
	void ITest.testStreams( Stream stmRead, Stream stmWrite )
	{
		throw new NotImplementedException();
	}
	void ITest.createFile( string str, out Stream stmWrite )
	{
		stmWrite = File.Create( str );
	}

	void IDisposable.Dispose()
	{
	}

	void ITest.testMarshalBack( string str, ITest managed )
	{
		throw new NotImplementedException();
	}
}