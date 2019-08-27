using System;

namespace ComLight
{
	/// <summary>Attribute to mark COM interfaces, equivalent to [Guid( "..." ), InterfaceType( ComInterfaceType.InterfaceIsIUnknown )] in the desktop .NET COM interop.</summary>
	[AttributeUsage( AttributeTargets.Interface, Inherited = false )]
	public class ComInterfaceAttribute: Attribute
	{
		public readonly Guid iid;

		public ComInterfaceAttribute( string iid )
		{
			this.iid = Guid.Parse( iid );
		}
	}
}