using System;

namespace ComLight
{
	/// <summary>Apply this attribute to COM interfaces to emit <see cref="System.Diagnostics.DebuggerTypeProxyAttribute" /> on the proxies which wrap C++ objects for .NET</summary>
	/// <remarks>This attribute only affects Visual Studio debugger, shouldn't affect runtime performance.<br />
	/// The type specified in constructor should have a public constructor which accepts the type of the interface.</remarks>
	[AttributeUsage( AttributeTargets.Interface, AllowMultiple = false )]
	public sealed class DebuggerTypeProxyAttribute: Attribute
	{
		internal readonly Type type;
		public DebuggerTypeProxyAttribute( Type type ) =>
			this.type = type ?? throw new ArgumentNullException( nameof( type ) );
	}
}