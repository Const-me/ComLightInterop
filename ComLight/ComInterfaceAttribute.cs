using System;

namespace ComLight
{
	/// <summary>Attribute to mark COM interfaces, equivalent to [Guid( "..." ), InterfaceType( ComInterfaceType.InterfaceIsIUnknown )] in the desktop .NET COM interop.</summary>
	[AttributeUsage( AttributeTargets.Interface, Inherited = false )]
	public class ComInterfaceAttribute: Attribute
	{
		/// <summary>COM interface ID for this interface, must match with the value in DEFINE_INTERFACE_ID macro in C++ code.</summary>
		public readonly Guid iid;

		/// <summary>Construct by parsing a string GUID</summary>
		public ComInterfaceAttribute( string iid )
		{
			this.iid = Guid.Parse( iid );
		}
	}
}