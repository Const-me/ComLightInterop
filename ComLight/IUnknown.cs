using System;
using System.Runtime.InteropServices;

namespace ComLight
{
	/// <summary>Native IUnknown stuff.</summary>
	static class IUnknown
	{
		[UnmanagedFunctionPointer( RuntimeClass.defaultCallingConvention )]
		public delegate int QueryInterface( IntPtr pThis, [In] ref Guid iid, out IntPtr result );

		[UnmanagedFunctionPointer( RuntimeClass.defaultCallingConvention )]
		public delegate uint AddRef( IntPtr pThis );

		[UnmanagedFunctionPointer( RuntimeClass.defaultCallingConvention )]
		public delegate uint Release( IntPtr pThis );

		public static readonly Guid iid = new Guid( "00000000-0000-0000-c000-000000000046" );

		public const int S_OK = 0;
		public const int S_FALSE = 1;
		public const int E_NOINTERFACE = unchecked((int)0x80004002L);
		public const int E_UNEXPECTED = unchecked((int)0x8000FFFFL);
	}
}