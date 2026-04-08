namespace ComLightGenerator.Emit;
using System;
using System.Collections.Generic;
using System.IO;

sealed class Marshaller: IDisposable
{
	readonly StreamWriter w;
	readonly IfaceMeta iface;
	readonly string name;
	readonly string visibility;
	readonly GeneratorMode mode;

	public Marshaller( StreamWriter w, in IfaceMeta iface, string visibility, GeneratorMode mode )
	{
		this.w = w;
		w.WriteLine();

		this.iface = iface;
		name = iface.marshallerType();
		this.visibility = visibility;
		this.mode = mode;
	}

	public void Dispose()
	{
		switch( mode )
		{
			case GeneratorMode.Net8:
				marshallerTypes( iface.name );
				w.WriteLine( "}" );
				w.WriteLine();
				w.WriteLine( "[NativeMarshalling( typeof( {0} ) )]", name );
				w.Write( "partial interface {0} {{ }}", iface.name );
				break;
			case GeneratorMode.Framework:
				marshallerMethods();
				w.Write( "}" );
				break;
			case GeneratorMode.Net8Internal:
			case GeneratorMode.FrameworkInternal:
				w.Write( "}" );
				break;
		}
	}

	public void addClass()
	{
		w.WriteLine( "/// <summary>Automatically generated marshaller for the ComLight interface <see cref=\"{0}\"/></summary>",
			iface.iface.str() );
		switch( mode )
		{
			case GeneratorMode.Net8:
				writeAttributes( w, iface.name, name );
				w.WriteLine( "{0} static unsafe class {1}", visibility, name );
				break;
			case GeneratorMode.Framework:
				w.WriteLine( "{0} sealed class {1}: ICustomMarshaler", visibility, name );
				break;
			case GeneratorMode.FrameworkInternal:
			case GeneratorMode.Net8Internal:
				w.WriteLine( "{0} static class {1}", visibility, name );
				break;
			default:
				throw new ArgumentException();
		}
		w.WriteLine( "{" );
		string iidString = iface.iid.ToString( "D" ).ToLowerInvariant();
		w.WriteLine( "	internal static readonly Guid s_iid = new Guid( \"{0}\" );", iidString );

		if( iface.direction != eMarshalDirection.ToManaged )
			managedWrapper( iface );

		w.WriteLine();
		w.WriteLine( "	/// <summary>Create callable proxy from unmanaged <see cref=\"{0}\" /> interface pointer</summary>", iface.iface.str() );
		w.WriteLine( "	{0} static {1}? toManaged( nint nativePointer, bool attach )", visibility, iface.name );
		w.WriteLine( "	{" );
		w.WriteLine( "		if( nativePointer == 0 ) return null;" );
		if( iface.direction != eMarshalDirection.ToNative )
			w.WriteLine( "		return {0}_proxy.create( nativePointer, attach );", iface.name );
		else
			w.WriteLine( "		throw new NotSupportedException( \"{0} doesn't support native to managed marshaling direction\" );",
				iface.iface.str() );
		w.WriteLine( "	}" );

		w.WriteLine();
		w.WriteLine( "	/// <summary>Create C++ compatible virtual table for the <see cref=\"{0}\" /> managed object</summary>", iface.iface.str() );
		w.Write( "	{0} static nint toNative( {1}? obj, bool addRef )", visibility, iface.name );
		if( iface.direction != eMarshalDirection.ToManaged )
		{
			w.WriteLine( " =>" );
			string methodName = ( iface.direction == eMarshalDirection.BothWays ) ? "wrapManagedBothWays" : "wrapManagedOneWay";
			w.WriteLine( "		ManagedWrapper.{0}<{1}>( obj, addRef, s_factory, s_iid );", methodName, iface.name );
		}
		else
		{
			w.WriteLine( "" );
			w.WriteLine( "	{" );
			w.WriteLine( "		if( null == obj )" );
			w.WriteLine( "			return 0;" );
			w.WriteLine( "		if( obj is RuntimeClass rc )" );
			w.WriteLine( "			return rc.nativePointer;" );
			w.WriteLine( "		throw new NotSupportedException( \"{0} doesn't support managed to native marshaling direction\" );",
				iface.iface.str() );
			w.WriteLine( "	}" );
		}
	}

	void managedWrapper( in IfaceMeta iface )
	{
		w.WriteLine();
		w.WriteLine( "	static Delegate[] managedDelegates( {0} obj )", iface.name );
		w.WriteLine( "	{" );
		ManagedWrapper.wrapManaged( w, iface );
		w.WriteLine( "	}" );
		w.WriteLine();
		w.WriteLine( "	static readonly Func<{0}, Delegate[]> s_factory = managedDelegates;", iface.name );
	}

	// Copy-pasted: https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/Marshalling/MarshalMode.cs
	// Impossible to reference, prehistoric .NET in the source generator
	enum MarshalMode
	{
		Default,
		/// <summary>By-value and <c>in</c> parameters in managed-to-unmanaged scenarios, like P/Invoke.</summary>
		ManagedToUnmanagedIn,
		/// <summary><c>ref</c> parameters in managed-to-unmanaged scenarios, like P/Invoke.</summary>
		ManagedToUnmanagedRef,
		/// <summary><c>out</c> parameters in managed-to-unmanaged scenarios, like P/Invoke.</summary>
		ManagedToUnmanagedOut,
		/// <summary>By-value and <c>in</c> parameters in unmanaged-to-managed scenarios, like Reverse P/Invoke.</summary>
		UnmanagedToManagedIn,
		/// <summary><c>ref</c> parameters in unmanaged-to-managed scenarios, like Reverse P/Invoke.</summary>
		UnmanagedToManagedRef,
		/// <summary><c>out</c> parameters in unmanaged-to-managed scenarios, like Reverse P/Invoke.</summary>
		UnmanagedToManagedOut,
		/// <summary>Elements of arrays passed with <c>in</c> or by-value in interop scenarios.</summary>
		ElementIn,
		/// <summary>Elements of arrays passed with <c>ref</c> or passed by-value with both <see cref="InAttribute"/> and <see cref="OutAttribute" /> in interop scenarios.</summary>
		ElementRef,
		/// <summary>Elements of arrays passed with <c>out</c> or passed by-value with only <see cref="OutAttribute" /> in interop scenarios.</summary>
		ElementOut
	}

	enum Impl: byte
	{
		NoRef,
		AddRef,
		Unsup,
	}

	static readonly Dictionary<MarshalMode, Impl> mappings = new()
	{
		{ MarshalMode.ManagedToUnmanagedIn, Impl.NoRef },
		// { MarshalMode.ManagedToUnmanagedRef, Impl.Unsup },
		{ MarshalMode.ManagedToUnmanagedOut, Impl.AddRef },

		{ MarshalMode.UnmanagedToManagedIn, Impl.NoRef },
		// { MarshalMode.UnmanagedToManagedRef, Impl.Unsup },
		{ MarshalMode.UnmanagedToManagedOut, Impl.AddRef },

		// { MarshalMode.ElementIn, Impl.NoRef },
		// { MarshalMode.ElementRef, Impl.Unsup },
		// { MarshalMode.ElementOut, Impl.NoRef },

		{ MarshalMode.Default, Impl.Unsup },
	};

	static void marshallerClass( StreamWriter w, Impl impl, string iface )
	{
		w.WriteLine( "	public static class {0}", impl );
		w.WriteLine( "	{" );

		if( impl != Impl.Unsup )
		{
			string addRef = ( impl == Impl.AddRef ) ? "true" : "false";

			w.WriteLine( "		public static {0}? ConvertToManaged( nint native ) =>", iface );
			w.WriteLine( "			toManaged( native, {0} );", addRef );

			w.WriteLine( "		public static nint ConvertToUnmanaged( {0}? obj ) =>", iface );
			w.WriteLine( "			toNative( obj, {0} );", addRef );
		}
		else
		{
			w.WriteLine( "		public static {0}? ConvertToManaged( nint native ) =>", iface );
			w.WriteLine( "			throw new NotSupportedException();" );

			w.WriteLine( "		public static nint ConvertToUnmanaged( {0}? obj ) =>", iface );
			w.WriteLine( "			throw new NotSupportedException();" );
		}
		w.WriteLine( "		public static void Free( nint native ) { }" );
		w.WriteLine( "	}" );
	}

	void marshallerTypes( string iface )
	{
		w.WriteLine();
		marshallerClass( w, Impl.NoRef, iface );
		marshallerClass( w, Impl.AddRef, iface );
		marshallerClass( w, Impl.Unsup, iface );
	}

	static void writeAttributes( StreamWriter w, string iface, string name )
	{
		foreach( var kvp in mappings )
		{
			w.WriteLine( "[CustomMarshaller( typeof({0}), MarshalMode.{1}, typeof( {2} ) )]",
				iface, kvp.Key, kvp.Value );
		}
	}

	void marshallerMethods()
	{
		w.WriteLine();
		w.WriteLine( "	void ICustomMarshaler.CleanUpManagedData( object obj ) { }" );
		w.WriteLine( "	void ICustomMarshaler.CleanUpNativeData( IntPtr ptr ) { }" );
		w.WriteLine( "	int ICustomMarshaler.GetNativeDataSize() => Marshal.SizeOf<IntPtr>();" );
		w.WriteLine( "	object ICustomMarshaler.MarshalNativeToManaged( IntPtr native ) => toManaged( native, true );" );
		w.WriteLine( "	IntPtr ICustomMarshaler.MarshalManagedToNative( object obj ) => toNative( obj as {0}, false );", iface.name );
		w.WriteLine();
		w.WriteLine( "	static readonly ICustomMarshaler s_instance = new {0}();", name );
		w.WriteLine( "	public static ICustomMarshaler GetInstance( string cookie ) => s_instance;" );
	}
}