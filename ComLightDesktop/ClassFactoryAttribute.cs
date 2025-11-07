namespace ComLight;
using System;

/// <summary>Apply to COM interface to generate a marshaller which implements 
/// <see cref="System.Runtime.InteropServices.ICustomMarshaler"/> interface compatible with <c>[DllImport]</c></summary>
[AttributeUsage( AttributeTargets.Interface )]
public sealed class ClassFactoryAttribute: Attribute
{
}