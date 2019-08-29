using System;

namespace ComLight
{
	/// <summary>Microsoft has weird defaults for string marshaling. Apply this attribute to marshal them as C strings, UTF16 on Windows, UTF8 on Linux.</summary>
	[AttributeUsage( AttributeTargets.Parameter )]
	public class NativeStringAttribute: Attribute
	{ }
}