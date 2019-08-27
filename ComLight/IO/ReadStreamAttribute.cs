using ComLight.IO;
using System;

namespace ComLight
{
	/// <summary>Apply on parameter of type <see cref="System.IO.Stream"/> to marshal into iReadStream native interface, allowing to read from streams implemented on the other side of the interop.</summary>
	[AttributeUsage( AttributeTargets.Parameter )]
	public class ReadStreamAttribute: MarshallerAttribute
	{
		public ReadStreamAttribute() :
			base( typeof( ReadStreamMarshal ) )
		{ }
	}
}