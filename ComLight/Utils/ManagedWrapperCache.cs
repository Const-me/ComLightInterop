using System;
using System.Runtime.CompilerServices;

namespace ComLight
{
	class ManagedWrapperCache<T> where T : class
	{
		readonly object syncRoot = new object();
		readonly ConditionalWeakTable<T, object> native = new ConditionalWeakTable<T, object>();
		readonly Func<T, IntPtr> factory;

		public ManagedWrapperCache( Func<T, IntPtr> f )
		{
			factory = f;
		}

		public IntPtr wrap( T managed )
		{
			lock( syncRoot )
			{
				object cached;
				if( native.TryGetValue( managed, out cached ) )
					return (IntPtr)cached;
				IntPtr result = factory( managed );
				native.Add( managed, result );
				return result;
			}
		}
	}
}