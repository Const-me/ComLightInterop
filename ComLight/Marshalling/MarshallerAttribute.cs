using System;

namespace ComLight
{
	/// <summary>Apply to interface parameters to implement custom marshaling.</summary>
	[AttributeUsage( AttributeTargets.Parameter )]
	public class MarshallerAttribute: Attribute
	{
		public readonly Type tMarshaller;

		public MarshallerAttribute( Type t )
		{
			if( !typeof( iCustomMarshal ).IsAssignableFrom( t ) )
				throw new ArgumentException( $"Marshaller type { t.FullName } must derive from iCustomMarshal abstract class." );
			tMarshaller = t;
		}
	}
}