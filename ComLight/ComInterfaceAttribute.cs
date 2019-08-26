using System;

namespace ComLight
{
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