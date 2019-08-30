using System;

namespace ComLight
{
	/// <summary>Apply to parameters to implement custom marshaling.</summary>
	[AttributeUsage( AttributeTargets.Parameter )]
	public class MarshallerAttribute: Attribute
	{
		/// <summary>The type that implements <see cref="iCustomMarshal" /></summary>
		public readonly Type tMarshaller;

		/// <summary>Construct with marshaller type, must implement <see cref="iCustomMarshal" /></summary>
		public MarshallerAttribute( Type t )
		{
			if( !typeof( iCustomMarshal ).IsAssignableFrom( t ) )
				throw new ArgumentException( $"Marshaller type { t.FullName } must derive from iCustomMarshal abstract class." );
			tMarshaller = t;
		}
	}
}