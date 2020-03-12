using System;
using System.Reflection;

namespace ComLight
{
	/// <summary>Utility class to cast objects between interfaces</summary>
	public static class ComLightCast
	{
		/// <summary>Cast the object to another COM interface</summary>
		/// <typeparam name="I">Result COM interface</typeparam>
		/// <param name="obj">The object to cast</param>
		/// <param name="releaseOldOne">If true, and the argument is a C++ implemented object, this method will release the old one.</param>
		/// <returns>The object casted to the requested COM interface</returns>
		public static I cast<I>( object obj, bool releaseOldOne = false ) where I : class
		{
			var type = typeof( I );
			if( !type.IsInterface )
				throw new InvalidCastException( "The type argument of the cast method must be an interface" );
			ComInterfaceAttribute attribute = type.GetCustomAttribute<ComInterfaceAttribute>();
			if( null == attribute )
				throw new InvalidCastException( "The type argument of the cast method must be an interface with [ComInterface] custom attribute" );

			if( null == obj )
				return null;

			if( obj is RuntimeClass runtimeClass )
			{
				// C++ implemented COM object
				if( obj is I result )
				{
					// The proxy already implements the interface. We're probably casting to a base interface.
					return result;
				}

				IntPtr newPointer;
				try
				{
					newPointer = runtimeClass.queryInterface( attribute.iid, true );
				}
				catch( Exception ex )
				{
					throw new InvalidCastException( "Unable to cast, the native object doesn't support the interface", ex );
				}
				finally
				{
					if( releaseOldOne )
						( (IDisposable)runtimeClass ).Dispose();
				}

				try
				{
					return (I)NativeWrapper.wrap( type, newPointer );
				}
				catch( Exception ex )
				{
					runtimeClass.release();
					throw new InvalidCastException( "Unable to cast, something's wrong with the interface", ex );
				}
			}

			if( obj is ManagedObject managedObject )
			{
				// C# implemented COM object
				if( managedObject.managed is I result )
					return result;

				throw new InvalidCastException( $"{ managedObject.managed.GetType().FullName } doesn't implement interface { type.FullName }" );
			}

			throw new InvalidCastException( $"{ obj.GetType().FullName } is not a ComLight object" );
		}
	}
}