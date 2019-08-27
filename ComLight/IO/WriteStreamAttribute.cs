using ComLight.IO;
using System;

namespace ComLight
{
	/// <summary>Apply on parameter of type <see cref="System.IO.Stream"/> to marshal into iWriteStream native interface, allowing C++ code to write .NET streams.</summary>
	[AttributeUsage( AttributeTargets.Parameter )]
	public class WriteStreamAttribute: MarshallerAttribute
	{
		public WriteStreamAttribute() :
			base( typeof( WriteStreamMarshal ) )
		{ }
	}
}