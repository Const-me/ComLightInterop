using System;

namespace ComLight
{
	/// <summary>Apply this attribute on string parameters to marshal as null-terminated C string, UTF-16 wchar_t on Windows, UTF-8 char on Linux.</summary>
	/// <remarks>This corresponds to LPCTSTR typedef on the native side of the interop.</remarks>
	[AttributeUsage( AttributeTargets.Parameter )]
	public class NativeStringAttribute: Attribute
	{ }
}