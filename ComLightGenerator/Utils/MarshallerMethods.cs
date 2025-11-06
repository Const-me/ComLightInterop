namespace ComLightGenerator;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

readonly struct MarshallerMethods
{
	public readonly string? prologue;
	public readonly string throwForHR;
	public readonly string throwAndReturnBool;
	public readonly string? managedException;

	public MarshallerMethods( INamedTypeSymbol? type )
	{
		prologue = null;
		throwForHR = "ErrorCodes.throwForHR";
		throwAndReturnBool = "ErrorCodes.throwAndReturnBool";

		if( null == type )
			return;

		prologue = findStaticMethod( type, "prologue", null, testPrologue );
		throwForHR = findStaticMethod( type, "throwForHR", throwForHR, testThrow )!;
		throwAndReturnBool = findStaticMethod( type, "throwAndReturnBool", throwAndReturnBool, testBool )!;
		managedException = findStaticMethod( type, "captureException", null, testException )!;

		static void testPrologue( INamedTypeSymbol type, IMethodSymbol method )
		{
			if( !method.ReturnType.isVoid() )
				throw new ArgumentException( $"The marshalling method {method.Name} in the class {type.str()} " +
					"should return void" );
			if( method.Parameters.Length != 0 )
				throw new ArgumentException( $"The marshalling method {method.Name} in the class {type.str()} " +
					"should not take any parameters" );
		}

		static void testThrow( INamedTypeSymbol type, IMethodSymbol method )
		{
			if( !method.ReturnType.isVoid() )
				throw new ArgumentException( $"The marshalling method {method.Name} in the class {type.str()} " +
					"should return void" );
			if( method.Parameters.Length != 1 || !method.Parameters[ 0 ].Type.isInt() )
				throw new ArgumentException( $"The marshalling method {method.Name} in the class {type.str()} " +
					"should take exactly one integer parameter" );
		}

		static void testBool( INamedTypeSymbol type, IMethodSymbol method )
		{
			if( !method.ReturnType.isBool() )
				throw new ArgumentException( $"The marshalling method {method.Name} in the class {type.str()} " +
					"should return bool" );
			if( method.Parameters.Length != 1 || !method.Parameters[ 0 ].Type.isInt() )
				throw new ArgumentException( $"The marshalling method {method.Name} in the class {type.str()} " +
					"should take exactly one integer parameter" );
		}

		static void testException( INamedTypeSymbol type, IMethodSymbol method )
		{
			if( !method.ReturnType.isVoid() )
				throw new ArgumentException( $"The marshalling method {method.Name} in the class {type.str()} " +
					"should return void" );
			do
			{
				if( method.Parameters.Length != 1 )
					break;
				if( method.Parameters[ 0 ].Type.str() != "System.Exception" )
					break;
				return;
			}
			while( false );
			throw new ArgumentException( $"The marshalling method {method.Name} in the class {type.str()} " +
				"should take exactly one parameter of type System.Exception" );
		}
	}

	static string? findStaticMethod( INamedTypeSymbol type, string name, string? def, Action<INamedTypeSymbol, IMethodSymbol> test )
	{
		ImmutableArray<ISymbol> arr = type.GetMembers( name );
		if( arr.IsEmpty )
			return def;
		string? result = null;
		foreach( ISymbol item in arr )
		{
			if( item.Kind != SymbolKind.Method )
				continue;
			IMethodSymbol method = (IMethodSymbol)item;
			if( !method.IsStatic )
				continue;
			test( type, method );
			if( !isAccessible( method ) )
				throw new ArgumentException( $"The marshalling method {name} in the class {type.str()} is inaccessible" );

			if( null == result )
			{
				result = method.Name;
				continue;
			}
			if( method.Name == result )
				continue;
			throw new ArgumentException( $"The class {type.str()} contains multiple marshaller methods \"{name}\"" );
		}

		if( null == result )
			return def;
		return $"{type.str()}.{result}";
	}

	static bool isAccessible( IMethodSymbol method )
	{
		switch( method.DeclaredAccessibility )
		{
			case Accessibility.Public:
			case Accessibility.Internal:
			case Accessibility.ProtectedOrInternal:
				return true;
			default:
				return false;
		}
	}
}