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

	public Marshaller( StreamWriter w, in IfaceMeta iface, string visibility )
	{
		this.w = w;
		w.WriteLine();

		this.iface = iface;
		name = iface.marshallerType();
		this.visibility = visibility;
	}

	public void Dispose()
	{
		w.WriteLine( "}" );
		w.WriteLine();
		w.WriteLine( "[NativeMarshalling( typeof( {0} ) )]", name );
		w.Write( "partial interface {0} {{ }}", iface.name );
	}

	public void addClass()
	{
		writeAttributes( w, iface.name, name );
		w.WriteLine( "{0} static unsafe class {1}", visibility, name );
		w.WriteLine( "{" );
		string iidString = iface.iid.ToString( "D" ).ToLowerInvariant();
		w.WriteLine( "	internal static readonly Guid s_iid = new Guid( \"{0}\" );", iidString );

		if( iface.direction != eMarshalDirection.ToManaged )
			managedWrapper( iface );

		w.WriteLine();
		w.WriteLine( "	static {0}? toManaged( nint nativePointer )", iface.name );
		w.WriteLine( "	{" );
		w.WriteLine( "		if( nativePointer == 0 ) return null;" );
		if( iface.direction != eMarshalDirection.ToNative )
			w.WriteLine( "		return {0}_proxy.create( nativePointer );", iface.name );
		else
			w.WriteLine( "		throw new NotSupportedException( \"{0} doesn't support native to managed marshaling direction\" );",
				iface.iface.str() );
		w.WriteLine( "	}" );

		w.WriteLine();
		w.Write( "	static nint toNative( {0}? obj, bool addRef )", iface.name );
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

		marshallerTypes( iface.name );
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
		{ MarshalMode.ManagedToUnmanagedOut, Impl.NoRef },

		{ MarshalMode.UnmanagedToManagedIn, Impl.NoRef },
		// { MarshalMode.UnmanagedToManagedRef, Impl.Unsup },
		{ MarshalMode.UnmanagedToManagedOut, Impl.AddRef },

		{ MarshalMode.ElementIn, Impl.NoRef },
		// { MarshalMode.ElementRef, Impl.Unsup },
		{ MarshalMode.ElementOut, Impl.NoRef },

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
			w.WriteLine( "			toManaged( native );" );

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
}