using System.Runtime.InteropServices;

namespace ComLight
{
	static partial class ErrorCodes
	{
		/// <summary>If the argument SUCCEEDED, do nothing. If it FAILED, throw an exception, such as <see cref="COMException"/>, resolving that code into message.</summary>
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