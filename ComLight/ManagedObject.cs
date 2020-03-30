using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace ComLight
{
	/// <summary>Implements a COM interface around managed object.</summary>
	/// <remarks>This class implements an equivalent of <see href="https://docs.microsoft.com/en-us/dotnet/standard/native-interop/com-callable-wrapper">COM Callable Wrapper</see></remarks>
	sealed class ManagedObject
	{
		/// <summary>COM interface pointer, just good enough for C++ to call the methods.</summary>
		public IntPtr address => gchNativeData.AddrOfPinnedObject();

		/// <summary>The managed object implementing that interface</summary>
		public readonly object managed;
		/// <summary>Pinned vtable data, plus one extra entry at the start.</summary>
		readonly GCHandle gchNativeData;
		/// <summary>If C++ code calls AddRef on the COM pointer, will use this GCHandle to protect the C# object from garbage collector.</summary>
		GCHandle gchManagedObject;
		/// <summary>Reference counter, it only counts references from C++ code.</summary>
		volatile int nativeRefCounter = 0;

		/// <summary>IUnknown function pointers</summary>
		readonly IUnknown.QueryInterface queryInterface;
		readonly IUnknown.AddRef addRef;
		readonly IUnknown.Release release;

		readonly Guid iid;
		readonly Delegate[] delegates;

		public ManagedObject( object managed, Guid iid, Delegate[] delegates )
		{
			this.managed = managed;
			this.iid = iid;

			IntPtr[] nativeTable = new IntPtr[ delegates.Length + 4 ];
			gchNativeData = GCHandle.Alloc( nativeTable, GCHandleType.Pinned );
			Cache.Managed.add( address, this );

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
				nativeTable[ i + 4 ] = Marshal.GetFunctionPointerForDelegate( delegates[ i ] );

			// Retain C# delegates for custom methods in the field of this class.
			// Failing to do so causes a runtime crash "A callback was made on a garbage collected delegate of type ComLight.Wrappers!…"
			this.delegates = delegates;
		}

		int implQueryInterface( [In] ref Guid ii, out IntPtr result )
		{
			if( ii == iid || ii == IUnknown.iid )
			{
				// From native code point of view, this COM object only supports 2 COM interfaces: IUnknown, and the one with the IID that was passed to the constructor.
				// In both cases, besides just returning the native pointer, we need to increment the ref.counter.
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
				// Retain the original user-provided object.
				// This ManagedObject wrapper is retained too, because ManagedWrapper.WrappersCache<I> has a ConditionalWeakTable for that.
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

				// This C#-implemented COM objects was retained and then released by C++ code.
				if( managed is iComDisposable cd )
				{
					try
					{
						cd.lastNativeReferenceReleased();
					}
					finally
					{
						gchManagedObject.Free();
					}
				}
				else
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
				Cache.Managed.drop( address );
				gchNativeData.Free();
			}
		}

		internal void callAddRef()
		{
			implAddRef();
		}
	};
}