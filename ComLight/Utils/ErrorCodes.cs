#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ComLight
{
	/// <summary>Utility functions for error handling</summary>
	public static partial class ErrorCodes
	{
		/// <summary>If the argument SUCCEEDED, do nothing. If it FAILED, throw an exception, such as <see cref="COMException"/>, resolving that code into message.</summary>
		/// <remarks>Very similar to <see cref="Marshal.ThrowExceptionForHR(int)" /> but supports more codes.</remarks>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static void throwForHR( int hr )
		{
			if( hr >= 0 )
				return; // SUCCEEDED
			throw getExceptionForHR( hr );
		}

		/// <summary>If the argument SUCCEEDED, interpret the value as boolean, 0 = S_OK = true, anything else = false. If FAILED, throw an exception, such as <see cref="COMException"/>, resolving that code into message.</summary>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static bool throwAndReturnBool( int hr )
		{
			if( hr >= 0 )
				return 0 == hr;
			throw getExceptionForHR( hr );
		}

		/// <summary>Get exception with a specific failure HRESULT value</summary>
		[MethodImpl( MethodImplOptions.NoInlining )]
		public static Exception getExceptionForHR( int hr )
		{
			Debug.Assert( hr < 0 );
			string? msg;
			if( codes.TryGetValue( hr, out msg ) )
				return new COMException( msg, hr );

#if NETCOREAPP
			Exception? ex = Marshal.GetExceptionForHR( hr, -1 );
			if( null == ex )
				return makeGeneric( hr );
			// Seems that boolean is the only way to detect NativeAOT, which as of .NET 10 has broken error handling.
			if( RuntimeFeature.IsDynamicCodeSupported )
				return ex;
			if( ex.GetType() != typeof( COMException ) )
				return ex;
			string? formatted = formatMessage( hr );
			if( null != formatted )
				return new COMException( formatted, hr );
			return ex;
#else
			Exception? ex = Marshal.GetExceptionForHR( hr );
			return ex ?? makeGeneric( hr );
#endif
		}

		static Exception makeGeneric( int hr )
		{
			string message = $"Unknown failed status 0x{hr:X8}";
			return new COMException( message, hr );
		}

#if NETCOREAPP
		/// <summary>Translate status code into message, or return null when unable to.</summary>
		static string? formatMessage( int hr )
		{
			if( OperatingSystem.IsWindows() )
				return WinApi.formatMessage( hr );
			return null;
		}

		/// <summary>Expose FormatMessage WinAPI to .NET. The API is supported by Windows for decades</summary>
		/// <remarks>Microsoft failed error handling in the AOT compiled version of the .NET 10 standard library.<br/>
		/// They forgot to call FormatMessage from <see cref="Marshal.GetExceptionForHR(int) " /> function.<br/>
		/// Instead, the function produces "Unexpected HRESULT has been returned from a call to a COM component" messages.
		/// The message is user hostile, and factually incorrect because ComLight only implements IUnknown-based ABI; these interfaces are not COM components.</remarks>
		static class WinApi
		{
			const string dll = "kernel32.dll";

			[DllImport( dll, ExactSpelling = true, SetLastError = true )]
			static extern int FormatMessageW( uint flags, nint source, int id, uint lang, out nint rdi, int length, nint args );

			[DllImport( dll, ExactSpelling = true, SetLastError = true )]
			static extern nint LocalFree( nint hMem );

			/// <summary>Translate status code into message, or return null when unable to.</summary>
			public static string? formatMessage( int hr )
			{
				const uint FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
				const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
				const uint FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
				const uint flags = FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS;

				int len = FormatMessageW( flags, 0, hr, 0, out nint buffer, 0, 0 );
				if( len == 0 || buffer == 0 )
					return null;

				string message;
				try
				{
					message = Marshal.PtrToStringUni( buffer, len );
				}
				finally
				{
					LocalFree( buffer );
				}

				if( string.IsNullOrWhiteSpace( message ) )
					return null;
				return message.Trim();
			}
		}
#endif
	}
}