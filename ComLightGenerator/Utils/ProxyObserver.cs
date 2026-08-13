using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
namespace ComLightGenerator;

/// <summary>Information about the optional [ProxyObserver] attribute which might be applied to COM interface methods</summary>
sealed class ProxyObserver
{
	/// <summary>If the method has [ProxyObserver], initialise the generator.<br/>Otherwise return null.</summary>
	public static ProxyObserver? create( IMethodSymbol method, in ComInterface iface )
	{
		AttributeData? attr = method.findAttribute( AttributeNames.proxyObserver );
		if( null == attr )
			return null;

		// Only allow for ToManaged and BothWays interfaces
		if( iface.direction == eMarshalDirection.ToNative )
			throw new ArgumentException( $"[ProxyObserver] applied to a method of {iface.name}, however the interface does not support ToManaged direction" );

		// Construct the object
		ImmutableArray<TypedConstant> ctorArgs = attr.ConstructorArguments;
		if( 2 != ctorArgs.Length )
			throw new ArgumentException( "Expected exactly 2 constructor arguments for [ProxyObserver]" );
		return new ProxyObserver( method, iface, ctorArgs );
	}

	/// <summary>Emit call to the observer from inside the generated proxy method</summary>
	public void emitCodes( StreamWriter w, in ComMethod mi )
	{
		int length = mi.parameters.Length;
		if( argumentsCount >= 0 )
			length = Math.Min( argumentsCount, length );

		w.Write( "\t\t{0}( this", method );
		for( int i = 0; i < length; i++ )
			w.Write( ", {0}", mi.parameters[ i ].name );
		w.WriteLine( " );" );
	}

	readonly string method;
	readonly sbyte argumentsCount;

	ProxyObserver( IMethodSymbol method, in ComInterface iface, ImmutableArray<TypedConstant> ctorArgs )
	{
		ITypeSymbol observerType = (ITypeSymbol)ctorArgs[ 0 ].Value!;
		string nameIface = iface.iface.MetadataName;
		string nameMethod = method.MetadataName;

		this.method = $"{observerType.ToDisplayString()}.{nameIface}_{nameMethod}";
		argumentsCount = (sbyte)ctorArgs[ 1 ].Value!;
	}
}