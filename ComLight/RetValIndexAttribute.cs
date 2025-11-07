using System;

namespace ComLight
{
	/// <summary>Apply this attribute to a method with C++ API HRESULT method( ISomething** result) if you want C# API ISomething method()</summary>
	[AttributeUsage( AttributeTargets.Method )]
	public sealed class RetValIndexAttribute: Attribute
	{
		/// <summary>Zero-based index of the RetVal argument in C++ projection of the COM interface.</summary>
		public readonly byte index;

		/// <summary>Constructor</summary>
		public RetValIndexAttribute( byte idx = 0 )
		{
			index = idx;
		}
	}
}