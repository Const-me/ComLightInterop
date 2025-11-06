namespace ComLightGenerator;
using ComLightGenerator.Emit;
using Microsoft.CodeAnalysis;
using System.Diagnostics;

static class GeneratorUtils
{
	/// <summary>Argument type including in/out/ref modifiers when necessary</summary>
	public static string argumentType( this IParameterSymbol ps )
	{
		string tp = ps.Type.str();
		switch( ps.RefKind )
		{
			case RefKind.None:
				return tp;
			case RefKind.Out:
				return $"out {tp}";
			case RefKind.In:
				if( ps.Type.IsValueType )
					return $"in {tp}";
				throw new ArgumentException();
			case RefKind.Ref:
				if( ps.Type.IsValueType )
					return $"ref {tp}";
				throw new ArgumentException();
			case RefKind.RefReadOnlyParameter:
				if( ps.Type.IsValueType )
					return $"ref readonly {tp}";
				throw new ArgumentException();
			default:
				throw new ArgumentException();
		}
	}

	static string baseName( this INamedTypeSymbol iface )
	{
		Debug.Assert( iface.isComInterface() );
		string name = iface.Name;
		if( char.ToLower( name[ 0 ] ) == 'i' )
			name = name.Substring( 1 );
		return name;
	}

	public static string marshallerType( this INamedTypeSymbol iface ) =>
		iface.baseName() + "Marshal";

	public static string marshallerType( this in IfaceMeta iface ) =>
		iface.iface.marshallerType();

	public static string? debuggerProxy( this in IfaceMeta iface )
	{
		var attr = iface.iface.findAttribute( AttributeNames.debuggerProxy );
		if( null == attr )
			return null;
		var arr = attr.ConstructorArguments;
		if( arr.Length != 1 )
			throw new ArgumentException( $"Malformed [DebuggerTypeProxy] attribute on {iface.iface.str()}" );
		ITypeSymbol ts = (ITypeSymbol)arr[ 0 ].Value!;
		return ts.str();
	}
}