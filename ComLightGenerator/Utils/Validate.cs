namespace ComLightGenerator;
using Microsoft.CodeAnalysis;

static class Validate
{
	public static void validate( this INamedTypeSymbol iface, GeneratorMode mode )
	{
		// Check for generic interfaces
		if( iface.IsGenericType )
			throw new ArgumentException( $"COM interface {iface.str()} is generic; this is not supported." );
		foreach( IMethodSymbol m in iface.getInterfaceMethods() )
			m.validate();
	}

	static void validate( this IMethodSymbol mi )
	{
		if( mi.IsGenericMethod )
			throw new ArgumentException( $"The method {mi.str()} is generic, this is not supported" );

		ITypeSymbol tRet = mi.ReturnType;
		if( mi.hasAttribute( AttributeNames.retValIndex ) )
		{
			// The return value is marshaled through an output parameter
			do
			{
				if( tRet.IsValueType ) break;
				if( tRet.isComInterface() ) break;
				throw new ArgumentException( $"The interface method {mi.str()} has unsupported return type {tRet.str()}, [RetValIndex] only supports value types or COM interfaces." );
			}
			while( false );
		}
		else if( !supportedReturnType( tRet ) )
			throw new ArgumentException( $"The interface method {mi.str()} has unsupported return type {tRet.str()}, must be void, int, bool or IntPtr" );

		foreach( IParameterSymbol pi in mi.Parameters )
			checkParameter( pi );
	}

	static bool supportedReturnType( ITypeSymbol type )
	{
		if( type == null )
			return false;
		if( type.isIntPtr() )
			return true;

		switch( type.SpecialType )
		{
			case SpecialType.System_Int32:
			case SpecialType.System_Boolean:
			case SpecialType.System_Void:
				return true;
		}
		return false;
	}

	static void checkParameter( IParameterSymbol pi )
	{
		AttributeData? custom = pi.findAttribute( "ComLight.MarshallerAttribute" );
		if( null != custom )
			throw new NotImplementedException();

		ITypeSymbol tp = pi.Type;
		if( tp.isComInterface() )
			return;

		if( pi.hasAttribute( "ComLight.NativeStringAttribute" ) )
		{
			switch( tp.ToDisplayString() )
			{
				case "string":
				case "System.Text.StringBuilder":
					return;
			}
			throw new ArgumentException( $"[NativeString] must be applied to a parameter of type string" );
		}

		if( pi.isByRef() )
		{
			if( tp.IsValueType )
				return;
			if( tp.TypeKind == TypeKind.Delegate )
				throw new ArgumentException( $"You can only pass delegates as input parameters" );
		}
	}
}