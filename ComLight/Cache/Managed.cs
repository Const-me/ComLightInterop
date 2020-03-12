using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ComLight.Cache
{
	/// <summary>Tracks live COM objects implemented in .NET.</summary>
	/// <remarks>Despite interfaces inheritance, each ManagedObject instance builds it's own COM vtable.
	/// If you construct multiple wrappers around different COM interfaces implemented by the same .NET objects, the wrappers will be unrelated to each other, you can't even use QueryInterface(IID_IUnknown) trick to detect they're implemented by the same object.
	/// That's why we don't need multimaps for this one.</remarks>
	static class Managed
	{
		static readonly object syncRoot = new object();

		/// <summary>COM objects constructed around C# objects</summary>
		static readonly Dictionary<IntPtr, WeakReference<ManagedObject>> managed = new Dictionary<IntPtr, WeakReference<ManagedObject>>();

		public static void add( IntPtr p, ManagedObject mo )
		{
			Debug.Assert( p != IntPtr.Zero );

			lock( syncRoot )
			{
				Debug.Assert( !managed.ContainsKey( p ) );
				managed.Add( p, new WeakReference<ManagedObject>( mo ) );
			}
		}

		public static bool drop( IntPtr p )
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

		public static ManagedObject lookup( IntPtr p )
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
			}
			throw new ApplicationException( $"Native COM pointer { p.ToString( "X" ) } is not on the cache" );
		}
	}
}