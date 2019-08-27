using System;

namespace ComLight
{
	/// <summary>Apply to interface parameters to specify custom marshaling</summary>
	[AttributeUsage( AttributeTargets.Parameter )]
	public class MarshallerAttribute: Attribute
	{
		public readonly Type tMarshaller;

		public MarshallerAttribute( Type t )
		{
			if( !typeof( iCustomMarshal ).IsAssignableFrom( t ) )
				throw new ArgumentException( $"Marshaller type { t.FullName } does not implements iCustomMarshal interface" );
			tMarshaller = t;
		}
	}
}