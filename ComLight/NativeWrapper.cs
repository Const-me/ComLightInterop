using System;
using System.Collections.Generic;

namespace ComLight
{
	/// <summary>Wraps C++ COM interfaces into dynamically built callable wrappers, derived from <see cref="RuntimeClass" />.</summary>
	static class NativeWrapper
	{
		// The factories are relatively expensive to build, reflection, dynamic compilation, that's why caching.
		static readonly object syncRoot = new object();
		static readonly Dictionary<Type, Func<IntPtr, object>> factories = new Dictionary<Type, Func<IntPtr, object>>();

		public static object wrap<I>( IntPtr native ) where I : class
		{
			Func<IntPtr, object> factory = null;
			lock( syncRoot )
			{
				if( !factories.TryGetValue( typeof( I ), out factory ) )
				{
					Type tp = typeof( I );
					Guid iid = ReflectionUtils.checkInterface( tp );
					factory = WrapInterface.build( tp, iid );
					factories.Add( typeof( I ), factory );
				}
			}
			return factory( native );
		}
	}
}