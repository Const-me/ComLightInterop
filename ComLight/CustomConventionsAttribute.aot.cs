namespace ComLight;
using System;

/// <summary>Apply to COM interface to use custom prologue function, and custom errors marshaling.</summary>
/// <remarks>Protip: C++ doesn’t feature async-await. You can keep per-thread error context in a static variable marked with [ThreadStatic] attribute.</remarks>
[AttributeUsage( AttributeTargets.Interface, Inherited = false )]
public class CustomConventionsAttribute: Attribute
{
	internal readonly Type type;
	/// <summary>Construct with the type implementing the conventions.</summary>
	public CustomConventionsAttribute( Type type ) => this.type = type;
}