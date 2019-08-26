using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace ComLight
{
	/// <summary>Implements a COM interface around managed object.</summary>
	class ManagedObject
	{
		public readonly IntPtr address;
		/// <summary>The managed object implementing that interface</summary>
		readonly object managed;
		/// <summary>Pinned vtable data, also it's address.</summary>
		GCHandle gchNativeData;
		/// <summary>If C++ code calls AddRef on the pointer, this class will pin the managed object in memory, saving it from garbage collector</summary>
		GCHandle gchManagedObject;
		/// <summary>Reference counter, it only counts references from C++ code.</summary>
		volatile int nativeRefCounter = 0;

		/// <summary>IUnknown function pointers</summary>
		readonly IUnknown.QueryInterface queryInterface;
		readonly IUnknown.AddRef addRef;
		readonly IUnknown.Release release;
		/// <summary>Custom methods function pointers</summary>
		readonly Delegate[] methodsDelegates;

		readonly Guid iid;

		public ManagedObject( object managed, Guid iid, Delegate[] delegates )
		{
			this.managed = managed;
			methodsDelegates = delegates;
			this.iid = iid;

			IntPtr[] nativeTable = new IntPtr[ delegates.Length + 4 ];
			gchNativeData = GCHandle.Alloc( nativeTable, GCHandleType.Pinned );

			address = gchNativeData.AddrOfPinnedObject();
			// In the 0-th element, write the address of the 1-st one. That's where vtable starts.
			nativeTable[ 0 ] = address + Marshal.SizeOf<IntPtr>();

			// Build 3 first entries of the vtable, with IUnknown methods
			queryInterface = delegate ( IntPtr pThis, ref Guid ii, out IntPtr result ) { Debug.Assert( pThis == address ); return implQueryInterface( ref ii, out result ); };
			nativeTable[ 1 ] = Marshal.GetFunctionPointerForDelegate( queryInterface );

			addRef = delegate ( IntPtr pThis ) { Debug.Assert( pThis == address ); return implAddRef(); };
			nativeTable[ 2 ] = Marshal.GetFunctionPointerForDelegate( addRef );

			release = delegate ( IntPtr pThis ) { Debug.Assert( pThis == address ); return implRelease(); };
			nativeTable[ 3 ] = Marshal.GetFunctionPointerForDelegate( release );

			// Custom entries of the vtable
			for( int i = 0; i < methodsDelegates.Length; i++ )
				nativeTable[ i + 4 ] = Marshal.GetFunctionPointerForDelegate( methodsDelegates[ i ] ); ;
		}

		int implQueryInterface( ref Guid ii, out IntPtr result )
		{
			if( ii == iid || ii == IUnknown.iid )
			{
				result = address;
				implAddRef();
				return IUnknown.S_OK;
			}
			result = IntPtr.Zero;
			return IUnknown.E_NOINTERFACE;
		}

		uint implAddRef()
		{
			int res = Interlocked.Increment( ref nativeRefCounter );
			if( 1 == res )
			{
				Debug.Assert( !gchManagedObject.IsAllocated );
				gchManagedObject = GCHandle.Alloc( managed );
			}
			return (uint)res;
		}

		uint implRelease()
		{
			int res = Interlocked.Decrement( ref nativeRefCounter );
			Debug.Assert( res >= 0 );
			if( 0 == res )
			{
				Debug.Assert( gchManagedObject.IsAllocated );
				gchManagedObject.Free();
			}
			return (uint)res;
		}

		~ManagedObject()
		{
			Debug.Assert( 0 == nativeRefCounter );
			Debug.Assert( !gchManagedObject.IsAllocated );

			if( gchManagedObject.IsAllocated )
				gchManagedObject.Free();

			if( gchNativeData.IsAllocated )
				gchNativeData.Free();
		}
	};
}