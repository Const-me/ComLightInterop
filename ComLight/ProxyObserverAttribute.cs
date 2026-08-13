using System;

namespace ComLight
{
	/// <summary>Apply to the method to inject observer call into generated proxy method.</summary>
	/// <remarks>The observer only work for ToManaged marshalling direction, not applicable for ToNative direction.<br/>
	/// When applied, the observer is called immediately before the native method</remarks>
	[AttributeUsage( AttributeTargets.Method, AllowMultiple = false )]
	public sealed class ProxyObserverAttribute: Attribute
	{
		/// <summary>Apply to a method to inject the observer</summary>
		public ProxyObserverAttribute( Type proxy, sbyte args = 0 ) { }
	}
}