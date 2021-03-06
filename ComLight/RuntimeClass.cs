﻿using System;
using System.Runtime.InteropServices;

namespace ComLight
{
	/// <summary>Abstract base class for generated proxies of C++ objects. Consumes IUnknown methods, implements IDisposable and finalizer.</summary>
	public abstract class RuntimeClass: IDisposable
	{
		/// <summary>For performance reason, runtime-generated derived classes access this field directly, without the overhead of property getter call.</summary>
		protected IntPtr m_nativePointer = IntPtr.Zero;
		/// <summary>Native COM pointer</summary>
		public IntPtr nativePointer => m_nativePointer;

		/// <summary>Calling convention for the interface methods.</summary>
		/// <remarks>Apparently StdCall is x86 only, on AMD64 something else is used instead. Fortunately, that something else appears to be binary compatible with both GCC and VC++ defaults.</remarks>
		public const CallingConvention defaultCallingConvention = CallingConvention.StdCall;

		readonly IUnknown.QueryInterface QueryInterface;
		readonly IUnknown.AddRef AddRef;
		readonly IUnknown.Release Release;

		/// <summary>Construct the wrapper.</summary>
		public RuntimeClass( IntPtr ptr, IntPtr[] vtbl, Guid iid )
		{
			m_nativePointer = ptr;
			this.iid = iid;
			QueryInterface = Marshal.GetDelegateForFunctionPointer<IUnknown.QueryInterface>( vtbl[ 0 ] );
			AddRef = Marshal.GetDelegateForFunctionPointer<IUnknown.AddRef>( vtbl[ 1 ] );
			Release = Marshal.GetDelegateForFunctionPointer<IUnknown.Release>( vtbl[ 2 ] );
			Cache.Native.add( ptr, this );
		}

		/// <summary>GUID of the COM interface</summary>
		public readonly Guid iid;

		/// <summary>Read the complete virtual methods table from the COM interface pointer.</summary>
		/// <param name="nativePointer">Native COM pointer</param>
		/// <param name="methodsCount">Count of methods in the C# interface. The COM interface has 3 more methods from IUnknown.</param>
		protected internal static IntPtr[] readVirtualTable( IntPtr nativePointer, int methodsCount )
		{
			IntPtr vtbl = Marshal.ReadIntPtr( nativePointer );
			int count = methodsCount + 3;

			IntPtr[] result = new IntPtr[ count ];
			Marshal.Copy( vtbl, result, 0, count );
			return result;
		}

		/// <summary>Release native COM pointer. If it reaches 0, causes C++ to run `delete this`. Safe to be called multiple times, only the first one will work.</summary>
		public void releaseInterfacePointer()
		{
			if( m_nativePointer != IntPtr.Zero )
			{
				Cache.Native.drop( m_nativePointer, this );
				Release( m_nativePointer );
				m_nativePointer = IntPtr.Zero;
			}
		}

		/// <summary>Release in finalizer.</summary>
		~RuntimeClass()
		{
			releaseInterfacePointer();
		}

		void IDisposable.Dispose()
		{
			releaseInterfacePointer();
			GC.SuppressFinalize( this );
		}

		internal IntPtr queryInterface( Guid iid, bool addRef )
		{
			int hr = QueryInterface( m_nativePointer, ref iid, out IntPtr result );
			ErrorCodes.throwForHR( hr );

			if( !addRef )
				Release( m_nativePointer );
			return result;
		}

		internal void addRef()
		{
			AddRef( m_nativePointer );
		}

		internal void release()
		{
			Release( m_nativePointer );
		}

		/// <summary>True if this proxy has a native COM pointer. Native pointers are released when you call IDisposable.Dispose()</summary>
		internal bool isAlive()
		{
			return m_nativePointer != IntPtr.Zero;
		}
	}
}