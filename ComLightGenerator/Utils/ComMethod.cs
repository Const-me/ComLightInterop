namespace ComLightGenerator;
using Microsoft.CodeAnalysis;

/// <summary>Defines what happens to return value of the methods</summary>
enum eMethodReturn: byte
{
	/// <summary><c>void</c> C# method, <c>HRESULT</c> in C++</summary>
	Void,
	/// <summary><c>bool</c> C# method, <c>HRESULT</c> in C++</summary>
	Bool,
	/// <summary><c>int</c> on both sides of the initerop, no exceptions</summary>
	Int,
	/// <summary>Native pointer on both sides of the initerop, no exceptions</summary>
	Pointer,

	/// <summary>The method has <c>[RetValIndex]</c>, C# projection returns a value type</summary>
	Value,
	/// <summary>The method has <c>[RetValIndex]</c>, C# projection returns a new COM object</summary>
	Object,
}

readonly struct ComMethod
{
	public readonly IMethodSymbol method;
	/// <summary>If the method has <c>[RetValIndex]</c>, the number from that attribute</summary>
	public readonly byte? retValIndex;
	/// <summary>Return type of the method</summary>
	public readonly eMethodReturn returns;
	public string name => method.Name;

	public readonly ComParameter[] parameters;
	/// <summary>The field is only set for <see cref="eMethodReturn.Object" />, <c>null</c> for the rest of them</summary>
	public readonly string? retValMarshaller;

	public ComMethod( IMethodSymbol method, in ComInterface iface )
	{
		this.method = method;
		AttributeData? attr = method.findAttribute( AttributeNames.retValIndex );
		if( null == attr )
			retValIndex = null;
		else
		{
			var args = attr.ConstructorArguments;
			if( args.Length <= 0 )
				retValIndex = 0;
			retValIndex = (byte)args[ 0 ].Value!;
		}

		ITypeSymbol rt = method.ReturnType;
		if( null == retValIndex )
		{
			if( !rt.isIntPtr() )
			{
				switch( rt.SpecialType )
				{
					case SpecialType.System_Int32:
						returns = eMethodReturn.Int;
						break;
					case SpecialType.System_Boolean:
						returns = eMethodReturn.Bool;
						break;
					case SpecialType.System_Void:
						returns = eMethodReturn.Void;
						break;
					default:
						throw new ArgumentException();
				}
			}
			else
				returns = eMethodReturn.Pointer;
		}
		else
		{
			if( rt.IsValueType )
				returns = eMethodReturn.Value;
			else if( rt.isComInterface() )
				returns = eMethodReturn.Object;
			else
				throw new ArgumentException();
		}

		var arr = method.Parameters;
		parameters = new ComParameter[ arr.Length ];
		for( int i = 0; i < arr.Length; i++ )
			parameters[ i ] = new ComParameter( arr[ i ], method );

		if( returns == eMethodReturn.Object )
		{
			string rvm = ( (INamedTypeSymbol)rt ).marshallerType();
			rvm = ReflectionUtils.typeName( rvm, rt.ContainingNamespace, iface.iface );
			retValMarshaller = rvm;
		}
	}

	public override string ToString() => method.ToDisplayString();
}

static class MethodUtils
{
	public static bool rawReturnType( this eMethodReturn mr ) =>
		mr == eMethodReturn.Int || mr == eMethodReturn.Pointer;
}