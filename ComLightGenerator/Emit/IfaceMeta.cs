namespace ComLightGenerator.Emit;
using Microsoft.CodeAnalysis;

readonly struct IfaceMeta
{
	public readonly INamedTypeSymbol iface;
	public readonly Guid iid;
	public readonly eMarshalDirection direction;

	public readonly MarshallerMethods marshaller;

	public readonly ComMethod[] methods;
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
	}
}