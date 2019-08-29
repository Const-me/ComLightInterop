using System;

namespace DesktopTest
{
	class Program
	{
		static void Main( string[] args )
		{
			try
			{
				// test0();
				// test1();
				// testStream();
				Tests.testMarshalBack();
			}
			catch( Exception ex )
			{
				Console.WriteLine( ex.ToString() );
			}
		}
	}
}