using System.Runtime.InteropServices;

namespace ComLight
{
	/// <summary>Support a few extra HRESULT codes missing from non-Windows versions of .NET Core</summary>
	public static partial class ErrorCodes
	{
		/// <summary>If the argument SUCCEEDED, do nothing. If it FAILED, throw an exception, such as <see cref="COMException"/>, resolving that code into message.</summary>
		/// <remarks>Very similar to <see cref="Marshal.ThrowExceptionForHR(int)" /> but supports more codes.</remarks>
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