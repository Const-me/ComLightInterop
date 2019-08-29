using System;
using System.Runtime.InteropServices;

namespace ComLight
{
	public class Marshaler<I>: ICustomMarshaler
		where I : class
	{
		readonly Guid iid;
		public Marshaler()
		{
			iid = ReflectionUtils.checkInterface( typeof( I ) );
		}

		void ICustomMarshaler.CleanUpManagedData( object ManagedObj )
		{
		}

		void ICustomMarshaler.CleanUpNativeData( IntPtr pNativeData )
		{
		}

		int ICustomMarshaler.GetNativeDataSize()
		{
			return Marshal.SizeOf<IntPtr>();
		}

		IntPtr ICustomMarshaler.MarshalManagedToNative( object ManagedObj )
		{
			// Build these vtables on top of the managed interface.
			return ManagedWrapper.wrap<I>( ManagedObj, false );
		}

		object ICustomMarshaler.MarshalNativeToManaged( IntPtr pNativeData )
		{
			if( pNativeData == IntPtr.Zero )
				return null;
			return NativeWrapper.wrap( typeof( I ), pNativeData );
		}

		static readonly ICustomMarshaler instance = new Marshaler<I>();

		public static ICustomMarshaler GetInstance( string pstrCookie )
		{
			return instance;
		}
	}
}