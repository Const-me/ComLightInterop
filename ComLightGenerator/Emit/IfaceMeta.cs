namespace ComLightGenerator.Emit;
using Microsoft.CodeAnalysis;

readonly struct IfaceMeta
{
	public readonly INamedTypeSymbol iface;
	public readonly Guid iid;
	public readonly eMarshalDirection direction;

	public readonly MarshallerMethods marshaller;

	public readonly ComMethod[] methods;

	/// <summary><c>true</c> if the COM interface is declared as partial</summary>
	/// <remarks>In .NET 8 generation mode, a partial interface indicates that the user intends to use C interop via <c>[LibraryImport]</c></remarks>
	public readonly bool isPartial;

	/// <summary><c>true</c> when the COM interface has [ClassFactory] attribute</summary>
	/// <remarks>In .NET framework generation mode, this attribute indicates that the user intends to use C interop via <c>[DllImport]</c></remarks>
	public readonly bool classFactory;
	public string name => iface.Name;

	public IfaceMeta( in ComInterface iface, in MarshallerMethods marshaller )
	{
		this.iface = iface.iface;
		iid = iface.iid;
		direction = iface.direction;
		this.marshaller = marshaller;

		var list = iface.iface.getInterfaceMethods().ToList();
		methods = new ComMethod[ list.Count ];
		for( int i = 0; i < list.Count; i++ )
			methods[ i ] = new ComMethod( list[ i ], iface );

		isPartial = iface.iface.isPartial();
		classFactory = iface.iface.hasAttribute( AttributeNames.classFactory );
	}
}