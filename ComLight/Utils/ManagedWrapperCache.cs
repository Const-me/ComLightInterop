using System;
using System.Runtime.CompilerServices;

namespace ComLight
{
	class ManagedWrapperCache<T> where T : class
	{
		readonly object syncRoot = new object();
		readonly ConditionalWeakTable<T, object> native = new ConditionalWeakTable<T, object>();
		readonly Func<T, bool, IntPtr> factory;

		public ManagedWrapperCache( Func<T, bool, IntPtr> f )
		{
			factory = f;
		}

		public IntPtr wrap( T managed, bool addRef )
		{
			lock( syncRoot )
			{
				object cached;
				IntPtr result;
				if( native.TryGetValue( managed, out cached ) )
				{
					result = (IntPtr)cached;
					if( addRef )
						LiveObjectsCache.addRef( result );
					return result;
				}
				result = factory( managed, addRef );
				native.Add( managed, result );
				return result;
			}
		}
	}
}