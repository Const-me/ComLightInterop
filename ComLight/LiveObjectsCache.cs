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
				return managed.Remove( p );
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

		static ManagedObject managedLookup( IntPtr p )
		{
			WeakReference<ManagedObject> wr;
			if( !managed.TryGetValue( p, out wr ) )
				return null;
			ManagedObject result;
			if( !wr.TryGetTarget( out result ) )
				return null;
			return result;
		}

		static RuntimeClass nativeLookup( IntPtr p )
		{
			WeakReference<RuntimeClass> wr;
			if( !native.TryGetValue( p, out wr ) )
				return null;
			RuntimeClass result;
			if( !wr.TryGetTarget( out result ) )
				return null;
			return result;
		}

		/// <summary>If `p` is the native COM pointer tracked by this class, call AddRef and return true.</summary>
		public static bool addRef( IntPtr p )
		{
			lock( syncRoot )
			{
				var mo = managedLookup( p );
				if( null != mo )
				{
					mo.callAddRef();
					return true;
				}
				var n = nativeLookup( p );
				if( null != n )
				{
					n.addRef();
					return true;
				}
			}
			throw new ApplicationException( $"Native COM pointer { p.ToString( "X" ) } was not on the cache" );
		}
	}
}