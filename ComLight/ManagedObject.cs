using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace ComLight
{
	/// <summary>Implements a COM interface around managed object.</summary>
	class ManagedObject
	{
		/// <summary>COM interface pointer, just good enough for C++ to call the methods.</summary>
		public IntPtr address => gchNativeData.AddrOfPinnedObject();

		/// <summary>The managed object implementing that interface</summary>
		readonly object managed;
		/// <summary>Pinned vtable data, plus one extra entry at the start.</summary>
		readonly GCHandle gchNativeData;
		/// <summary>If C++ code calls AddRef on the pointer, this class will use this GCHandle to protect managed object from garbage collector.</summary>
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

			lock( syncRoot )
				instances.Add( address, new WeakReference<ManagedObject>( this ) );

			// A COM pointer is an address of address: "this" points to vtable pointer, vtable pointer points to the first vtable entry, the rest of the entries follow.
			// We want binary compatibility, so nativeTable[ 0 ] contains address of nativeTable[ 1 ], and methods function pointers start at nativeTable[ 1 ].
			nativeTable[ 0 ] = address + Marshal.SizeOf<IntPtr>();

			// Build 3 first entries of the vtable, with IUnknown methods
			queryInterface = delegate ( IntPtr pThis, ref Guid ii, out IntPtr result ) { Debug.Assert( pThis == address ); return implQueryInterface( ref ii, out result ); };
			nativeTable[ 1 ] = Marshal.GetFunctionPointerForDelegate( queryInterface );

			addRef = delegate ( IntPtr pThis ) { Debug.Assert( pThis == address ); return implAddRef(); };
			nativeTable[ 2 ] = Marshal.GetFunctionPointerForDelegate( addRef );

			release = delegate ( IntPtr pThis ) { Debug.Assert( pThis == address ); return implRelease(); };
			nativeTable[ 3 ] = Marshal.GetFunctionPointerForDelegate( release );

			// Custom methods entries of the vtable
			for( int i = 0; i < delegates.Length; i++ )
				nativeTable[ i + 4 ] = Marshal.GetFunctionPointerForDelegate( delegates[ i ] ); ;
		}

		int implQueryInterface( [In] ref Guid ii, out IntPtr result )
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
			{
				lock( syncRoot )
					instances.Remove( address );
				gchNativeData.Free();
			}
		}

		internal void callAddRef()
		{
			implAddRef();
		}

		static readonly object syncRoot = new object();
		static readonly Dictionary<IntPtr, WeakReference<ManagedObject>> instances = new Dictionary<IntPtr, WeakReference<ManagedObject>>();

		internal static ManagedObject tryGetInstance( IntPtr p )
		{
			WeakReference<ManagedObject> wr;
			lock( syncRoot )
			{
				if( !instances.TryGetValue( p, out wr ) )
					return null;
			}
			ManagedObject result;
			if( !wr.TryGetTarget( out result ) )
				return null;
			return result;
		}
	};
}