using System;

namespace ComLight
{
	/// <summary>Direction of the interface marshaling</summary>
	public enum eMarshalDirection: byte
	{
		/// <summary>Expose C++ objects to .NET</summary>
		ToManaged,
		/// <summary>Expose .NET objects to C++</summary>
		ToNative,
		/// <summary>Marshal objects both ways</summary>
		BothWays,
	}

	/// <summary>Attribute to mark COM interfaces, equivalent to [Guid( "..." ), InterfaceType( ComInterfaceType.InterfaceIsIUnknown )] in the desktop .NET COM interop.</summary>
	[AttributeUsage( AttributeTargets.Interface, Inherited = false )]
	public class ComInterfaceAttribute: Attribute
	{
		/// <summary>COM interface ID for this interface, must match the value in DEFINE_INTERFACE_ID macro in C++ code.</summary>
		public readonly Guid iid;

		/// <summary>You can limit the direction of the marshaling, e.g. it makes no sense to implement something like ID3D11Buffer in C#, it won't work.</summary>
		/// <remarks>Single-direction marshaling is more efficient than the default <see cref="eMarshalDirection.BothWays" />.</remarks>
		public readonly eMarshalDirection marshalDirection;

		/// <summary>Construct by parsing a string GUID</summary>
		public ComInterfaceAttribute( string iid, eMarshalDirection direction = eMarshalDirection.BothWays )
		{
			this.iid = Guid.Parse( iid );
			marshalDirection = direction;
		}
	}
}