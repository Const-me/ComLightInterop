using System;
using System.Runtime.CompilerServices;

namespace ComLight
{
	/// <summary>A weakly-referenced cache with <c>T</c> keys, and tuples of <c>( IntPtr, Impl )</c> values</summary>
	/// <remarks>Used to implement wrappers around C# streams passed to C++ methods</remarks>
	sealed class ManagedWrapperCache<T, Impl> where T : class where Impl : class
	{
		readonly object syncRoot = new object();

		/// <summary>Values cached in this class</summary>
		public sealed class Entry
		{
			public readonly IntPtr nativePointer;
			readonly Impl impl;

			public Entry( IntPtr nativePointer, Impl impl )
			{
				this.nativePointer = nativePointer;
				this.impl = impl;
			}
		}

		readonly ConditionalWeakTable<T, Entry> native = new ConditionalWeakTable<T, Entry>();
		readonly Func<T, bool, Entry> factory;

		public ManagedWrapperCache( Func<T, bool, Entry> f )
		{
			factory = f;
		}

		public IntPtr wrap( T managed, bool addRef )
		{
			if( null == managed )
				return IntPtr.Zero;

			lock( syncRoot )
			{
				Entry entry;
				if( native.TryGetValue( managed, out entry ) )
				{
					if( addRef )
						Cache.Managed.addRef( entry.nativePointer );
					return entry.nativePointer;
				}
				entry = factory( managed, addRef );
				native.Add( managed, entry );
				return entry.nativePointer;
			}
		}
	}
}