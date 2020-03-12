using System;
using System.Collections.Generic;
using System.Diagnostics;
using WeakRef = System.WeakReference<ComLight.RuntimeClass>;
using WeakRefSet = System.Collections.Generic.HashSet<System.WeakReference<ComLight.RuntimeClass>>;

namespace ComLight.Cache
{
	// Desktop version of ConditionalWeakTable doesn't implement IEnumerable. Implementing same functionality using much slower code, with a hash set of weak references.
	// Let's hope not too many people are going to use the desktop version of this library, MS .NET built-in COM interop ain't that bad after all.
	static class Native
	{
		static readonly object syncRoot = new object();
		static readonly Dictionary<IntPtr, WeakRefSet> native = new Dictionary<IntPtr, WeakRefSet>();

		public static void add( IntPtr p, RuntimeClass rc )
		{
			Debug.Assert( p != IntPtr.Zero );

			lock( syncRoot )
			{
				WeakRefSet set;
				if( !native.TryGetValue( p, out set ) )
				{
					set = new WeakRefSet();
					native.Add( p, set );
				}
				set.Add( new WeakRef( rc ) );
			}
		}

		static readonly List<WeakRef> deadRefs = new List<WeakRef>();

		public static bool drop( IntPtr p, RuntimeClass rc )
		{
			lock( syncRoot )
			{
				WeakRefSet set;
				if( !native.TryGetValue( p, out set ) )
					return false;

				bool found = false;
				foreach( WeakRef wr in set )
				{
					if( !wr.TryGetTarget( out RuntimeClass r ) )
					{
						deadRefs.Add( wr );
						continue;
					}

					if( r == rc )
					{
						found = true;
						deadRefs.Add( wr );
					}
					else if( !rc.isAlive() )
						deadRefs.Add( wr );
				}

				foreach( var wr in deadRefs )
					set.Remove( wr );
				deadRefs.Clear();

				if( set.Count <= 0 )
					native.Remove( p );

				return found;
			}
		}

		public static RuntimeClass lookup( IntPtr p, Type tInterface )
		{
			lock( syncRoot )
			{
				WeakRefSet set;
				if( !native.TryGetValue( p, out set ) )
					return null;

				foreach( WeakRef wr in set )
				{
					if( !wr.TryGetTarget( out RuntimeClass rc ) )
						continue;
					if( !rc.isAlive() )
						continue;
					if( !tInterface.IsAssignableFrom( rc.GetType() ) )
						continue;
					return rc;
				}
			}
			return null;
		}
	}
}