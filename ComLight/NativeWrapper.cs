using ComLight.Emit;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ComLight
{
	/// <summary>Wraps C++ COM interfaces into dynamically built callable wrappers, derived from <see cref="RuntimeClass" />.</summary>
	public static class NativeWrapper
	{
		// The factories are relatively expensive to build: reflection, dynamic compilation, other shenanigans.
		// Also the name of dynamically built classes only depend on the interface, building 2 factories for the same interface would result in name conflict.
		// That's why caching.
		static readonly object syncRoot = new object();
		static readonly Dictionary<Type, Func<IntPtr, object>> factories = new Dictionary<Type, Func<IntPtr, object>>();

		/// <summary>Create a factory which supports two-way marshaling.</summary>
		static Func<IntPtr, object> createTwoWayFactory( Type tInterface )
		{
			Func<IntPtr, object> newProxy = Proxy.build( tInterface );

			return ( IntPtr pNative ) =>
			{
				if( pNative == IntPtr.Zero )
					return null;

				RuntimeClass rc = LiveObjectsCache.nativeLookup( pNative );
				if( null != rc )
					return rc;

				ManagedObject mo = LiveObjectsCache.managedLookup( pNative );
				if( null != mo )
					return mo.managed;

				return newProxy( pNative );
			};
		}

		/// <summary>Create a factory which only supports objects implemented in C++</summary>
		static Func<IntPtr, object> createOneWayToManagedFactory( Type tInterface )
		{
			Func<IntPtr, object> newProxy = Proxy.build( tInterface );

			return ( IntPtr pNative ) =>
			{
				if( pNative == IntPtr.Zero )
					return null;

				RuntimeClass rc = LiveObjectsCache.nativeLookup( pNative );
				if( null != rc )
					return rc;

				return newProxy( pNative );
			};
		}

		/// <summary>Create a factory which only supports objects implemented in .NET. Will throw an exception when given a COM object which is not implemented in C#.</summary>
		static Func<IntPtr, object> createOneWayToNativeFactory( Type tInterface )
		{
			string directionNotSupportedError = $"The COM interface { tInterface.FullName } doesn't support native to managed marshaling direction";

			return ( IntPtr pNative ) =>
			{
				if( pNative == IntPtr.Zero )
					return null;
				ManagedObject mo = LiveObjectsCache.managedLookup( pNative );
				if( null != mo )
					return mo.managed;
				throw new NotSupportedException( directionNotSupportedError );
			};
		}

		/// <summary>Having an interface type, return a factory that wraps C++ com pointer into .NET object.</summary>
		/// <remarks>Factories are relatively expensive to create, and are cached in this class.</remarks>
		public static Func<IntPtr, object> getFactory( Type tInterface )
		{
			Func<IntPtr, object> factory = null;
			lock( syncRoot )
			{
				if( factories.TryGetValue( tInterface, out factory ) )
					return factory;

				var attr = tInterface.GetCustomAttribute<ComInterfaceAttribute>();
				if( null == attr )
					throw new ArgumentException( $"The type { tInterface.FullName } doesn't have [ComInterface] applied." );

				switch( attr.marshalDirection )
				{
					case eMarshalDirection.BothWays:
						factory = createTwoWayFactory( tInterface );
						break;
					case eMarshalDirection.ToManaged:
						factory = createOneWayToManagedFactory( tInterface );
						break;
					case eMarshalDirection.ToNative:
						factory = createOneWayToNativeFactory( tInterface );
						break;
					default:
						throw new ArgumentException( $"Unexpected eMarshalDirection value { (byte)attr.marshalDirection }" );
				}
				factories.Add( tInterface, factory );
				return factory;
			}
		}

		/// <summary>Wrap native COM interface pointer into .NET object</summary>
		/// <param name="tInterface">COM interface .NET type</param>
		/// <param name="nativeComPointer">Native COM object pointer</param>
		public static object wrap( Type tInterface, IntPtr nativeComPointer )
		{
			if( nativeComPointer == IntPtr.Zero )
				return null;    // Best case performance-wise, BTW
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