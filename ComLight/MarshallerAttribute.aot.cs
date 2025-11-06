namespace ComLight;
using System;

/// <summary>Apply to parameters to implement custom marshaling.</summary>
[AttributeUsage( AttributeTargets.Parameter )]
public sealed class MarshallerAttribute: Attribute
{
	/// <summary>The type with <c>[CustomMarshaller]</c> attribute[s] applied</summary>
	public readonly Type tMarshaller;

	/// <summary>Construct with marshaller type, must contain <c>[CustomMarshaller]</c> attribute[s]</summary>
	public MarshallerAttribute( Type t ) =>
		tMarshaller = t;
}