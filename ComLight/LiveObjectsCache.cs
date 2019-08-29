using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ComLight
{
	/// <summary>Tracks live COM objects.</summary>
	static class LiveObjectsCache
	{
		static readonly object syncRoot = new object();

		/// <summary>COM objects constructed around C# objects</summary>
		static readonly Dictionary<IntPtr, WeakReference<ManagedObject>> managed = new Dictionary<IntPtr, WeakReference<ManagedObject>>();

		/// <summary>COM objects implemented in C++ code.</summary>
		static readonly Dictionary<IntPtr, WeakReference<RuntimeClass>> native = new Dictionary<IntPtr, WeakReference<RuntimeClass>>();

		public static void managedAdd( IntPtr p, ManagedObject mo )
		{
			Debug.Assert( p != IntPtr.Zero );

			lock( syncRoot )
			{
				Debug.Assert( !managed.ContainsKey( p ) );
				managed.Add( p, new WeakReference<ManagedObject>( mo ) );
			}
		}

		public static bool managedDrop( IntPtr p )
		{
			lock( syncRoot )
			{
				WeakReference<ManagedObject> wr;
				if( !managed.TryGetValue( p, out wr ) )
					return false;
				if( wr.isDead() )
					managed.Remove( p );
				// If the weak reference is alive, it means the COM pointer address was reused for another object.
				// The managedDrop is called by ManagedObject finalizer.
				// Finalizers run long after weak references expire: http://www.philosophicalgeek.com/2014/08/20/short-vs-long-weak-references-and-object-resurrection/
				// It's possible by the time finalizer is running, C++ code already constructed different object with the same COM pointer, and passed it to .NET.
				return true;
			}
		}

		public static void nativeAdd( IntPtr p, RuntimeClass rc )
		{
			Debug.Assert( p != IntPtr.Zero );

			lock( syncRoot )
			{
				Debug.Assert( !native.ContainsKey( p ) );
				native.Add( p, new WeakReference<RuntimeClass>( rc ) );
			}
		}

		public static bool nativeDrop( IntPtr p )
		{
			lock( syncRoot )
				return native.Remove( p );
		}

		public static RuntimeClass nativeLookup( IntPtr p )
		{
			WeakReference<RuntimeClass> wr;
			lock( syncRoot )
			{
				if( !native.TryGetValue( p, out wr ) )
					return null;
			}
			return wr.getTarget();
		}

		public static ManagedObject managedLookup( IntPtr p )
		{
			WeakReference<ManagedObject> wr;
			lock( syncRoot )
			{
				if( !managed.TryGetValue( p, out wr ) )
					return null;
			}
			return wr.getTarget();
		}

		/// <summary>If `p` is the native COM pointer tracked by this class, call AddRef. Otherwise throw an exception.</summary>
		public static void addRef( IntPtr p )
		{
			lock( syncRoot )
			{
				var mo = managed.lookup( p )?.getTarget();
				if( null != mo )
				{
					mo.callAddRef();
					return;
				}
				var n = native.lookup( p )?.getTarget();
				if( null != n )
				{
					n.addRef();
					return;
				}
			}
			throw new ApplicationException( $"Native COM pointer { p.ToString( "X" ) } is not on the cache" );
		}
	}
}