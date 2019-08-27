using System;
using System.Runtime.InteropServices;

namespace ComLight
{
	/// <summary>Abstract base class for generated proxies. Consumes IUnknown methods, implements IDisposable and finalizer.</summary>
	public abstract class RuntimeClass: IDisposable
	{
		public readonly IntPtr nativePointer = IntPtr.Zero;

		public const CallingConvention defaultCallingConvention = CallingConvention.StdCall;

		readonly IUnknown.QueryInterface QueryInterface;
		// readonly IUnknown.AddRef AddRef;
		readonly IUnknown.Release Release;

		public RuntimeClass( IntPtr ptr, IntPtr[] vtbl, Guid iid )
		{
			nativePointer = ptr;
			this.iid = iid;
			QueryInterface = Marshal.GetDelegateForFunctionPointer<IUnknown.QueryInterface>( vtbl[ 0 ] );
			// AddRef = Marshal.GetDelegateForFunctionPointer<IUnknown.AddRef>( vtbl[ 1 ] );
			Release = Marshal.GetDelegateForFunctionPointer<IUnknown.Release>( vtbl[ 2 ] );
		}

		/// <summary>GUID of the COM interface</summary>
		public readonly Guid iid;

		/// <summary>Read the complete virtual methods table from the COM interface pointer.</summary>
		/// <param name="methodsCount">Count of methods in the C# interface. The COM interface has 3 more methods from IUnknown.</param>
		protected static IntPtr[] readVirtualTable( IntPtr nativePointer, int methodsCount )
		{
			IntPtr vtbl = Marshal.ReadIntPtr( nativePointer );
			int count = methodsCount + 3;

			IntPtr[] result = new IntPtr[ count ];
			Marshal.Copy( vtbl, result, 0, count );
			return result;
		}

		bool pointerReleased = false;

		public void releaseInterfacePointer()
		{
			if( !pointerReleased )
			{
				pointerReleased = true;
				if( nativePointer != IntPtr.Zero )
					Release( nativePointer );
			}
		}

		~RuntimeClass()
		{
			releaseInterfacePointer();
		}

		void IDisposable.Dispose()
		{
			releaseInterfacePointer();
			GC.SuppressFinalize( this );
		}

		internal IntPtr queryInterface( Guid iid )
		{
			IntPtr result = IntPtr.Zero;
			int hr = QueryInterface( nativePointer, ref iid, out result );
			Marshal.ThrowExceptionForHR( hr );
			// This method is internal, it's used by WrapManaged class. It only needs to cast interfaces, it doesn't expect to retain the new one.
			// Calling Release() to negate the effect of QueryInterface.
			Release( nativePointer );
			return result;
		}
	}
}