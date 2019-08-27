using ComLight.IO;
using System;

namespace ComLight
{
	/// <summary>Apply on parameter of type <see cref="System.IO.Stream"/> to marshal into iReadStream native interface, allowing C++ code to read .NET streams.</summary>
	[AttributeUsage( AttributeTargets.Parameter )]
	public class ReadStreamAttribute: MarshallerAttribute
	{
		public ReadStreamAttribute() :
			base( typeof( ReadStreamMarshal ) )
		{ }
	}
}