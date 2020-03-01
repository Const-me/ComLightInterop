using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ComLight
{
	/// <summary>Wraps managed interfaces into COM objects callable by native code.</summary>
	public static partial class ManagedWrapper
	{
		/// <summary>When native code doesn't bother calling AddRef on these interfaces, the lifetime of the wrappers is linked to the lifetime of the interface objects. This class implements that link.</summary>
		static class WrappersCache<I> where I : class
		{
			static readonly ConditionalWeakTable<I, ManagedObject> table = new ConditionalWeakTable<I, ManagedObject>();

			public static void add( I obj, ManagedObject wrapper )
			{
				table.Add( obj, wrapper );
			}

			public static IntPtr? lookup( I obj )
			{
				ManagedObject result;
				if( !table.TryGetValue( obj, out result ) )
					return null;
				return result.address;
			}
		}

		static readonly object syncRoot = new object();
		static readonly Dictionary<Type, Func<object, bool, IntPtr>> cache = new Dictionary<Type, Func<object, bool, IntPtr>>();

		/// <summary>Create a factory which supports two-way marshaling.</summary>
		static Func<object, bool, IntPtr> createTwoWayFactory<I>( Guid iid ) where I : class
		{
			// The builder gets captured by the lambda.
			// This is what we want, the constructor takes noticeable time, the code outside the lambda runs once per interface type, the code inside lambda runs once per object instance.
			InterfaceBuilder builder = new InterfaceBuilder( typeof( I ) );

			return ( object obj, bool addRef ) =>
			{
				if( null == obj )
				{
					// Marshalling null
					return IntPtr.Zero;
				}
				Debug.Assert( obj is I );

				if( obj is RuntimeClass rc )
				{
					// That .NET object is not actually managed, it's a wrapper around C++ implemented COM interface.
					if( rc.iid == iid )
					{
						// It wraps around the same interface
						if( addRef )
							rc.addRef();
						return rc.nativePointer;
					}

					// It wraps around different interface. Call QueryInterface on the native object.
					return rc.queryInterface( iid, addRef );
				}

				// It could be the same managed object is reused across native calls. If that's the case, the cache already contains the native pointer.
				I managed = (I)obj;
				IntPtr? wrapped = WrappersCache<I>.lookup( managed );
				if( wrapped.HasValue )
					return wrapped.Value;

				Delegate[] delegates = builder.compile( managed );
				ManagedObject wrapper = new ManagedObject( managed, iid, delegates );
				WrappersCache<I>.add( managed, wrapper );
				if( addRef )
					wrapper.callAddRef();
				return wrapper.address;
			};
		}

		/// <summary>Create a factory which only supports objects implemented in .NET</summary>
		static Func<object, bool, IntPtr> createOneWayToNativeFactory<I>( Guid iid ) where I : class
		{
			// The builder gets captured by the lambda.
			// This is what we want, the constructor takes noticeable time, the code outside the lambda runs once per interface type, the code inside lambda runs once per object instance.
			InterfaceBuilder builder = new InterfaceBuilder( typeof( I ) );

			return ( object obj, bool addRef ) =>
			{
				if( null == obj )
				{
					// Marshalling null
					return IntPtr.Zero;
				}
				Debug.Assert( obj is I );

				// It could be the same managed object is reused across native calls. If that's the case, the cache already contains the native pointer.
				I managed = (I)obj;
				IntPtr? wrapped = WrappersCache<I>.lookup( managed );
				if( wrapped.HasValue )
					return wrapped.Value;

				Delegate[] delegates = builder.compile( managed );
				ManagedObject wrapper = new ManagedObject( managed, iid, delegates );
				WrappersCache<I>.add( managed, wrapper );
				if( addRef )
					wrapper.callAddRef();
				return wrapper.address;
			};
		}

		/// <summary>Create a factory which only supports objects implemented in C++</summary>
		static Func<object, bool, IntPtr> createOneWayToManagedFactory( Type tInterface, Guid iid )
		{
			string directionNotSupportedError = $"The COM interface { tInterface.FullName } doesn't support managed to native marshaling direction";

			return ( object obj, bool addRef ) =>
			{
				if( null == obj )
				{
					// Marshalling null
					return IntPtr.Zero;
				}

				if( obj is RuntimeClass rc )
				{
					// That .NET object is not actually managed, it's a wrapper around C++ implemented COM interface. We can marshal these just fine.
					if( rc.iid == iid )
					{
						// It wraps around the same interface
						if( addRef )
							rc.addRef();
						return rc.nativePointer;
					}

					// It wraps around different interface. Call QueryInterface on the native object.
					return rc.queryInterface( iid, addRef );
				}

				throw new NotSupportedException( directionNotSupportedError );
			};
		}

		static Func<object, bool, IntPtr> getFactory<I>() where I : class
		{
			Type tInterface = typeof( I );
			Func<object, bool, IntPtr> result;
			lock( syncRoot )
			{
				if( cache.TryGetValue( tInterface, out result ) )
					return result;

				Guid iid = ReflectionUtils.checkInterface( tInterface );

				var attr = tInterface.GetCustomAttribute<ComInterfaceAttribute>();
				if( null == attr )
					throw new ArgumentException( $"The type { tInterface.FullName } doesn't have [ComInterface] applied." );

				switch( attr.marshalDirection )
				{
					case eMarshalDirection.BothWays:
						result = createTwoWayFactory<I>( iid );
						break;
					case eMarshalDirection.ToManaged:
						result = createOneWayToManagedFactory( tInterface, iid );
						break;
					case eMarshalDirection.ToNative:
						result = createOneWayToNativeFactory<I>( iid );
						break;
					default:
						throw new ArgumentException( $"Unexpected eMarshalDirection value { (byte)attr.marshalDirection }" );
				}

				cache.Add( tInterface, result );
				return result;
			}
		}

		/// <summary>Wrap a C# interface into a COM object callable from native code</summary>
		/// <typeparam name="I">COM interface type</typeparam>
		/// <param name="obj">COM interface instance</param>
		/// <param name="addRef">Pass True to increment native ref.counter, do that when you want to move ownership to C++ code.</param>
		/// <returns>Native COM pointer of the wrapper</returns>
		public static IntPtr wrap<I>( object obj, bool addRef ) where I : class
		{
			Func<object, bool, IntPtr> factory = getFactory<I>();
			return factory( obj, addRef );
		}
	}
}