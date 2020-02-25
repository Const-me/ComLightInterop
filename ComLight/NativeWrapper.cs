using ComLight.Emit;
using System;
using System.Collections.Generic;

namespace ComLight
{
	/// <summary>Wraps C++ COM interfaces into dynamically built callable wrappers, derived from <see cref="RuntimeClass" />.</summary>
	public static class NativeWrapper
	{
		// The factories are relatively expensive to build, reflection, dynamic compilation.
		// Also the name of dynamically built classes only depend on the interface, building 2 factories for the same interface would result in name conflict.
		// That's why caching.
		static readonly object syncRoot = new object();
		static readonly Dictionary<Type, Func<IntPtr, object>> factories = new Dictionary<Type, Func<IntPtr, object>>();

		/// <summary>Having an interface type, return a factory that wraps C++ com pointer into .NET object.</summary>
		/// <remarks>Factories are relatively expensive to create, and are cached in this class.</remarks>
		public static Func<IntPtr, object> getFactory( Type tInterface )
		{
			Func<IntPtr, object> factory = null;
			lock( syncRoot )
			{
				if( factories.TryGetValue( tInterface, out factory ) )
					return factory;
				Func<IntPtr, object> newProxy = Proxy.build( tInterface );
				factory = ( IntPtr pNative ) =>
				{
					RuntimeClass rc = LiveObjectsCache.nativeLookup( pNative );
					if( null != rc )
						return rc;
					ManagedObject mo = LiveObjectsCache.managedLookup( pNative );
					if( null != mo )
						return mo.managed;
					return newProxy( pNative );
				};
				factories.Add( tInterface, factory );
				return factory;
			}
		}

		/// <summary>Wrap native COM interface pointer into .NET object</summary>
		/// <param name="tInterface">COM interface .NET type</param>
		/// <param name="nativeComPointer">Native COM object pointer</param>
		public static object wrap( Type tInterface, IntPtr nativeComPointer )
		{
			Func<IntPtr, object> factory = getFactory( tInterface );
			return factory( nativeComPointer );
		}

		/// <summary>Wrap native COM interface pointer into .NET object</summary>
		/// <typeparam name="I">COM interface .NET type</typeparam>
		/// <param name="nativeComPointer">Native COM object pointer</param>
		public static I wrap<I>( IntPtr nativeComPointer ) where I : class
		{
			return (I)wrap( typeof( I ), nativeComPointer );
		}
	}
}