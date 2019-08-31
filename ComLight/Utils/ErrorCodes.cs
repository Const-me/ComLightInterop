using System.Runtime.InteropServices;

namespace ComLight
{
	static partial class ErrorCodes
	{
		public static void throwForHR( int hr )
		{
			if( hr >= 0 )
				return; // SUCCEEDED
			string msg;
			if( codes.TryGetValue( hr, out msg ) )
				throw new COMException( msg, hr );
			Marshal.ThrowExceptionForHR( hr );
		}
	}
}