#nullable enable
namespace ComLight;
using System;

public static partial class ManagedWrapper
{
	/// <summary>Called by generated codes to create C++ COM objects from C# objects, 
	/// for <see cref="eMarshalDirection.BothWays" /> marshaling direction</summary>
	public static nint wrapManagedBothWays<I>( I? obj, bool addRef, Func<I, Delegate[]> factory, in Guid iid )
		where I : class
	{
		if( null == obj )
			return 0;

		if( obj is RuntimeClass rc )
		{
			if( addRef )
				rc.addRef();
			return rc.nativePointer;
		}

		ManagedObject mo = WrappersCache<I>.lookupManaged( obj );
		if( null == mo )
		{
			mo = new ManagedObject( obj, iid, factory( obj ) );
			WrappersCache<I>.add( obj, mo );
		}
		if( addRef )
			mo.callAddRef();
		return mo.address;
	}

	/// <summary>Called by generated codes to create C++ COM objects from C# objects, 
	/// for <see cref="eMarshalDirection.ToNative" /> marshaling direction</summary>
	public static nint wrapManagedOneWay<I>( I? obj, bool addRef, Func<I, Delegate[]> factory, in Guid iid )
		where I : class
	{
		if( null == obj )
			return 0;

		ManagedObject mo = WrappersCache<I>.lookupManaged( obj );
		if( null == mo )
		{
			mo = new ManagedObject( obj, iid, factory( obj ) );
			WrappersCache<I>.add( obj, mo );
		}
		if( addRef )
			mo.callAddRef();
		return mo.address;
	}
}