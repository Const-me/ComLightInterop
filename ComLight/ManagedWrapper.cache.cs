using System;
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

			public static ManagedObject lookupManaged( I obj )
			{
				ManagedObject result;
				if( !table.TryGetValue( obj, out result ) )
					return null;
				return result;
			}
		}

		internal static ManagedObject lookupManaged<I>( I obj ) where I : class =>
			WrappersCache<I>.lookupManaged( obj );
	}
}