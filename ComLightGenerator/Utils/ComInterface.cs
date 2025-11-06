namespace ComLightGenerator;
using Microsoft.CodeAnalysis;

/// <summary>Direction of the interface marshaling</summary>
enum eMarshalDirection: byte
{
	/// <summary>Expose C++ objects to .NET</summary>
	ToManaged,
	/// <summary>Expose .NET objects to C++</summary>
	ToNative,
	/// <summary>Marshal objects both ways</summary>
	BothWays,
}

readonly struct ComInterface
{
	public readonly INamedTypeSymbol iface;
	public readonly Guid iid;
	public readonly eMarshalDirection direction;
	public readonly INamedTypeSymbol? conventions;
	public string name => iface.Name;

	public ComInterface( INamedTypeSymbol iface, Guid iid, eMarshalDirection direction )
	{
		this.iface = iface;
		this.iid = iid;
		this.direction = direction;

		AttributeData? attr = iface.findAttribute( AttributeNames.conventions );
		if( null != attr && attr.ConstructorArguments.Length > 0 )
		{
			TypedConstant tc = attr.ConstructorArguments[ 0 ];
			conventions = (INamedTypeSymbol)tc.Value!;
		}
	}
}