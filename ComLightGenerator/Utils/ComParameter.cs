namespace ComLightGenerator;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

[StructLayout( LayoutKind.Auto )]
readonly struct ComParameter
{
	public readonly IParameterSymbol symbol;
	public readonly AttributeData? marshalAs;
	public readonly bool isComInterface;
	public readonly bool isBool;
	public readonly bool isArray;
	public readonly bool isOutput;
	readonly bool hasInAttribute;
	public bool isInput => !isOutput;
	public readonly string nativeType;
	public readonly string? marshalUsing;

	public string name => symbol.Name;

	public string nativeArgType()
	{
		string tp = nativeType;
		switch( symbol.RefKind )
		{
			case RefKind.None:
				return tp;
			case RefKind.Out:
				return $"out {tp}";
			case RefKind.In:
				if( symbol.Type.IsValueType )
					return $"in {tp}";
				throw new ArgumentException();
			case RefKind.Ref:
				if( symbol.Type.IsValueType )
					return $"ref {tp}";
				throw new ArgumentException();
			case RefKind.RefReadOnlyParameter:
				if( symbol.Type.IsValueType )
					return $"ref readonly {tp}";
				throw new ArgumentException();
			default:
				throw new ArgumentException();
		}
	}

	public ComParameter( IParameterSymbol symbol, IMethodSymbol method )
	{
		this.symbol = symbol;
		marshalAs = findMarshalAs( symbol );
		hasInAttribute = symbol.hasAttribute( AttributeNames.input );

		ITypeSymbol type = symbol.Type;
		isComInterface = type.isComInterface();
		if( isComInterface )
		{
			if( !goodObjectDirection( symbol ) )
				throw new ArgumentException( $"Unable to marshal {symbol.Name} parameter of {method.str()}: " +
					$"unsupported direction {symbol.RefKind}" );
			isOutput = symbol.RefKind != RefKind.None;

			marshalUsing = ( (INamedTypeSymbol)type ).marshallerType();
			nativeType = "nint";
			return;
		}

		if( type.str() == "System.IO.Stream" )
		{
			marshalUsing = marshalStream( symbol, method );
			nativeType = "nint";
			isOutput = symbol.RefKind != RefKind.None;
			return;
		}

		isBool = ( type.SpecialType == SpecialType.System_Boolean );
		if( isBool )
		{
			nativeType = "bool";
			return;
		}

		nativeType = type.str();

		isArray = type.isArray();
		if( isArray )
		{
			ITypeSymbol e = type.arrayElementType();
			if( !e.IsValueType )
				throw new ArgumentException( $"Unable to marshal {symbol.Name} parameter of {method.str()}: " +
					"the parameter is an array, and the elements are not value types" );
		}
	}

	static AttributeData? findMarshalAs( IParameterSymbol symbol )
	{
		ImmutableArray<AttributeData> arr = symbol.GetAttributes();
		if( arr.IsEmpty )
			return null;
		return arr.FirstOrDefault( isMarshalAs );

		static bool isMarshalAs( AttributeData a ) =>
			a.AttributeClass?.str() == AttributeNames.marshalAs;
	}

	static bool goodObjectDirection( IParameterSymbol symbol )
	{
		switch( symbol.RefKind )
		{
			case RefKind.None:
			case RefKind.Out:
				return true;
			default:
				return false;
		}
	}

	static string marshalStream( IParameterSymbol symbol, IMethodSymbol method )
	{
		if( !goodObjectDirection( symbol ) )
			throw new ArgumentException( $"Unable to marshal Stream parameter of {method.str()}: " +
				$"unsupported direction {symbol.RefKind}" );

		byte bitmap = 0;
		foreach( var a in symbol.GetAttributes() )
		{
			switch( a.AttributeClass?.str() )
			{
				case AttributeNames.readStream:
					bitmap |= 1;
					break;
				case AttributeNames.writeStream:
					bitmap |= 2;
					break;
			}
		}

		if( 0 == bitmap )
			throw new ArgumentException( $"Unable to marshal Stream parameter of {method.str()}: " +
				"you should apply either [ReadStream] or [WriteStream] custom attribute to the parameter" );
		if( 3 == bitmap )
			throw new ArgumentException( $"Unable to marshal Stream parameter of {method.str()}: " +
				"you should not apply both [ReadStream] and [WriteStream] custom attributes to the parameter" );

		if( 1 == bitmap )
			return "ComLight.IO.ReadStreamMarshaller";
		else
		{
			Debug.Assert( 2 == bitmap );
			return "ComLight.IO.WriteStreamMarshaller";
		}
	}

	string argumentModifier()
	{
		switch( symbol.RefKind )
		{
			case RefKind.None:
			case RefKind.In:
				return string.Empty;
			case RefKind.Out:
				return "out ";
			case RefKind.Ref:
				if( symbol.Type.IsValueType )
					return "ref ";
				throw new ArgumentException();
			case RefKind.RefReadOnlyParameter:
				if( symbol.Type.IsValueType )
					return "ref readonly ";
				throw new ArgumentException();
			default:
				throw new ArgumentException();
		}
	}

	public bool nativePostProcessing()
	{
		if( null != marshalUsing )
			return true;
		return nativeKeepAlive();
	}

	public bool nativeKeepAlive()
	{
		if( null != marshalUsing )
			return isInput;
		if( symbol.Type.IsValueType )
			return false;
		switch( symbol.Type.TypeKind )
		{
			case TypeKind.Delegate:
			case TypeKind.FunctionPointer:
				return true;
		}
		return false;
	}

	public string managedArgumentModifier() => argumentModifier();
	public string nativeArgumentModifier() => argumentModifier();

	public string nativeInputMarshaller()
	{
		string marshalUsing = this.marshalUsing!;
		Debug.Assert( !isOutput );
		return $"{marshalUsing}.NoRef.ConvertToUnmanaged";
	}

	public string nativeOutputMarshaller()
	{
		string marshalUsing = this.marshalUsing!;
		Debug.Assert( isOutput );
		return $"{marshalUsing}.NoRef.ConvertToManaged";
	}

	public string managedInputMarshaller()
	{
		string marshalUsing = this.marshalUsing!;
		Debug.Assert( !isOutput );
		return $"{marshalUsing}.NoRef.ConvertToManaged";
	}

	public string managedOutputMarshaller()
	{
		string marshalUsing = this.marshalUsing!;
		Debug.Assert( isOutput );
		return $"{marshalUsing}.AddRef.ConvertToUnmanaged";
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder();
		sb.Append( symbol.ToString() );
		if( null != marshalUsing )
			sb.AppendFormat( ": native {0}, marshaller {1}", nativeType, marshalUsing );
		return sb.ToString();
	}

	/// <summary><c>true</c> when user specified [In] attribute and it makes sense to propagate to the delegate</summary>
	public bool emitInAttrtibute()
	{
		if( !hasInAttribute )
			return false;
		if( !symbol.Type.IsValueType )
			return false;
		if( !isInput )
			return false;
		switch( symbol.RefKind )
		{
			case RefKind.Ref:
			case RefKind.In:
			case RefKind.RefReadOnlyParameter:
				return true;
			default:
				return false;
		}
	}
}