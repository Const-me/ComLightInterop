namespace ComLightGenerator.Emit;
using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics;
using System.IO;

sealed class ProxyBuilder: IDisposable
{
	readonly StreamWriter w;

	public ProxyBuilder( StreamWriter w )
	{
		this.w = w;
		w.WriteLine();
	}

	public void Dispose()
	{
		w.WriteLine( "}" );
	}

	public void addClass( in IfaceMeta iface )
	{
		string? debuggerProxy = iface.debuggerProxy();
		if( null != debuggerProxy )
			w.WriteLine( "[System.Diagnostics.DebuggerTypeProxy( typeof( {0} ) )]", debuggerProxy );

		string name = $"{iface.name}_proxy";
		w.WriteLine( "sealed class {0}: RuntimeClass, {1}", name, iface.name );
		w.WriteLine( "{" );
		w.WriteLine( "	internal static {0} create( nint nativePointer, bool attach ) =>", name );
		w.WriteLine( "		new {0}( nativePointer, readVirtualTable( nativePointer, {1} ), attach );",
			name, iface.methods.Length );
	}

	public void addConstructor( in IfaceMeta iface )
	{
		w.WriteLine();
		string dels = $"{iface.name}_native";
		foreach( var method in iface.methods )
			w.WriteLine( "	readonly {0}.{1} m_{1};", dels, method.name );

		string name = $"{iface.name}_proxy";
		w.WriteLine();
		w.WriteLine( "	{0}( nint nativePointer, IntPtr[] vtbl, bool attach ):", name );
		w.WriteLine( "		base( nativePointer, vtbl, attach, {0}.s_iid )", iface.marshallerType() );
		w.WriteLine( "	{" );
		for( int i = 0; i < iface.methods.Length; i++ )
		{
			string mi = iface.methods[ i ].name;
			w.WriteLine( "		m_{1} = Marshal.GetDelegateForFunctionPointer<{0}.{1}>( vtbl[ {2} ] );",
				dels, mi, i + 3 );
		}
		w.WriteLine( "	}" );
	}

	public void addMethod( in IfaceMeta iface, in ComMethod mi )
	{
		w.WriteLine();
		w.Write( "	{0} ", mi.method.ReturnType.str() );
		w.Write( "{0}.{1}(", iface.name, mi.name );

		var arr = mi.parameters;
		for( int i = 0; i < arr.Length; i++ )
		{
			if( i == 0 )
				w.Write( " " );
			else
				w.Write( ", " );
			w.Write( "{0} {1}", arr[ i ].symbol.argumentType(), arr[ i ].name );
		}
		if( arr.Length > 0 )
			w.Write( " " );
		w.WriteLine( ")" );
		w.WriteLine( "	{" );
		if( null != iface.marshaller.prologue && !mi.returns.rawReturnType() )
			w.WriteLine( "		{0}();", iface.marshaller.prologue );

		bool postProcessing = mi.parameters.Any( pi => pi.nativePostProcessing() );
		bool needClose;
		bool haveRetVal = mi.retValIndex.HasValue;
		if( postProcessing )
		{
			switch( mi.returns )
			{
				default:
					w.Write( "		{0}( ", iface.marshaller.throwForHR );
					needClose = true;
					break;
				case eMethodReturn.Bool:
					w.Write( "		bool _retVal = {0}( ", iface.marshaller.throwAndReturnBool );
					needClose = true;
					haveRetVal = true;
					break;
				case eMethodReturn.Int:
					w.Write( "		int _retVal = " );
					needClose = false;
					haveRetVal = true;
					break;
				case eMethodReturn.Pointer:
					w.Write( "		nint _retVal = " );
					needClose = false;
					haveRetVal = true;
					break;
			}
		}
		else
		{
			switch( mi.returns )
			{
				default:
					w.Write( "		{0}( ", iface.marshaller.throwForHR );
					needClose = true;
					break;
				case eMethodReturn.Bool:
					w.Write( "		return {0}( ", iface.marshaller.throwAndReturnBool );
					needClose = true;
					break;
				case eMethodReturn.Int:
				case eMethodReturn.Pointer:
					w.Write( "		return " );
					needClose = false;
					break;
			}
		}

		w.Write( "m_{0}( m_nativePointer", mi.name );

		int retValIndex = -1;
		if( mi.retValIndex.HasValue )
			retValIndex = mi.retValIndex.Value;
		for( int i = 0; i < arr.Length; i++ )
		{
			if( i == retValIndex )
			{
				retValIndex = -1;
				w.Write( ", out var _retVal" );
				i--;
				continue;
			}

			string? marshal = arr[ i ].marshalUsing;
			if( null == marshal )
			{
				w.Write( ", {0}{1}", arr[ i ].nativeArgumentModifier(), arr[ i ].name );
				continue;
			}
			if( arr[ i ].isOutput )
			{
				w.Write( ", out var _{0}", arr[ i ].name );
				continue;
			}
			w.Write( ", {0}( {1} )", arr[ i ].nativeInputMarshaller(), arr[ i ].name );
		}
		if( retValIndex >= 0 )
			w.Write( ", out var _retVal" );

		w.Write( " )" );
		if( needClose )
			w.Write( " )" );
		w.WriteLine( ";" );

		foreach( ComParameter param in mi.parameters )
		{
			if( param.nativeKeepAlive() )
				w.WriteLine( "		GC.KeepAlive( {0} );", param.name );

			string? marshal = param.marshalUsing;
			if( null == marshal || param.isInput )
				continue;
			w.WriteLine( "		{0} = {1}( _{0} );", param.name, param.nativeOutputMarshaller() );
		}

		if( haveRetVal )
		{
			if( mi.returns != eMethodReturn.Object )
				w.WriteLine( "		return _retVal;" );
			else
			{
				ITypeSymbol retType = mi.method.ReturnType;
				Debug.Assert( retType.isComInterface() );
				INamedTypeSymbol named = (INamedTypeSymbol)retType;
				string mt = named.marshallerType();
				INamespaceSymbol nss = named.ContainingNamespace;
				bool sameNamespace = SymbolEqualityComparer.Default.Equals( nss, iface.iface.ContainingNamespace );
				string ns = string.Empty;
				if( !nss.IsGlobalNamespace && !sameNamespace )
					ns = $"{nss.str()}.";
				w.WriteLine( "		return {0}{1}.NoRef.ConvertToManaged( _retVal );", ns, mt );
			}
		}
		w.WriteLine( "	}" );
	}
}