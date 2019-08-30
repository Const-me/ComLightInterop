using System;
using System.Runtime.InteropServices;

namespace ComLight
{
	/// <summary>Marshaller which wraps native COM pointer into callable wrapper, or constructs C++ vtables around .NET objects</summary>
	/// <typeparam name="I">COM interface, must be marked with <see cref="ComInterfaceAttribute" />.</typeparam>
	public class Marshaler<I>: ICustomMarshaler
		where I : class
	{
		readonly Guid iid;
		/// <summary></summary>
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

		/// <summary>In addition to implementing the ICustomMarshaler interface, custom marshalers must implement a static method called GetInstance that accepts a String as a parameter and has a return type of ICustomMarshaler.</summary>
		public static ICustomMarshaler GetInstance( string pstrCookie )
		{
			return instance;
		}
	}
}