using ComLight.Emit;
using System;
using System.Collections.Generic;

namespace ComLight
{
	/// <summary>Wraps C++ COM interfaces into dynamically built callable wrappers, derived from <see cref="RuntimeClass" />.</summary>
	static class NativeWrapper
	{
		// The factories are relatively expensive to build, reflection, dynamic compilation.
		// Also the name of dynamically built classes only depend on the interface, building 2 factories for the same interface would result in name conflict.
		// That's why caching.
		static readonly object syncRoot = new object();
		static readonly Dictionary<Type, Func<IntPtr, object>> factories = new Dictionary<Type, Func<IntPtr, object>>();

		static Func<IntPtr, object> getFactory( Type tInterface )
		{
			Func<IntPtr, object> factory = null;
			lock( syncRoot )
			{
				if( factories.TryGetValue( tInterface, out factory ) )
					return factory;
				factory = Proxy.build( tInterface );
				factories.Add( tInterface, factory );
				return factory;
			}
		}

		public static object wrap( Type tInterface, IntPtr nativeComPointer )
		{
			Func<IntPtr, object> factory = getFactory( tInterface );
			return factory( nativeComPointer );
		}

		public static I wrap<I>( IntPtr nativeComPointer ) where I : class
		{
			return (I)wrap( typeof( I ), nativeComPointer );
		}
	}
}