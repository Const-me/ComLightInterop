namespace ComLight;
using System;

/// <summary>Apply on parameter of type <see cref="System.IO.Stream"/> to marshal into iWriteStream native interface, allowing to write streams implemented on the other side of the interop.</summary>
[AttributeUsage( AttributeTargets.Parameter )]
public sealed class WriteStreamAttribute: Attribute
{
}