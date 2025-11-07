namespace ComLightGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

static class ReflectionUtils
{
	public static AttributeData? findAttribute( this ISymbol sym, string fullName )
	{
		foreach( AttributeData attr in sym.GetAttributes() )
		{
			if( attr.AttributeClass == null )
				continue;
			if( attr.AttributeClass.str() != fullName )
				continue;
			return attr;
		}
		return null;
	}

	public static bool hasAttribute( this ISymbol sym, string fullName ) =>
		null != sym.findAttribute( fullName );

	/// <summary>True if the parameter is passed by reference (ref/out/in)</summary>
	public static bool isByRef( this IParameterSymbol param ) =>
		param.RefKind != RefKind.None;
	public static bool isVoid( this ITypeSymbol type ) =>
		type.SpecialType == SpecialType.System_Void;
	public static bool isInt( this ITypeSymbol type ) =>
		type.SpecialType == SpecialType.System_Int32;
	public static bool isBool( this ITypeSymbol type ) =>
		type.SpecialType == SpecialType.System_Boolean;

	public static bool isIntPtr( this ITypeSymbol type )
	{
		if( type.SpecialType == SpecialType.System_IntPtr )
			return true;
		if( type.ToDisplayString() == "System.IntPtr" )
			return true;
		return false;
	}

	public static bool isComInterface( this ITypeSymbol ts )
	{
		if( ts.TypeKind != TypeKind.Interface )
			return false;
		return ts.hasAttribute( AttributeNames.comInterface );
	}

	public static bool isArray( this ITypeSymbol ts ) =>
		ts is IArrayTypeSymbol;

	public static ITypeSymbol arrayElementType( this ITypeSymbol ts )
	{
		Debug.Assert( ts.isArray() );
		return ( (IArrayTypeSymbol)ts ).ElementType;
	}

	public static string str( this ISymbol sym ) => sym.ToDisplayString();

	/// <summary>Returns all methods of the interface excluding property accessors.</summary>
	public static IEnumerable<IMethodSymbol> getInterfaceMethods( this INamedTypeSymbol iface )
	{
		foreach( var member in iface.GetMembers() )
		{
			if( member is IMethodSymbol method )
			{
				if( method.MethodKind != MethodKind.Ordinary )
					continue;
				yield return method;
			}
		}
	}

	/// <summary>Make a simple pluralized string with the count, e.g. <c>"2 cats"</c></summary>
	/// <remarks>Handles basic English pluralization; does not cover irregular forms like "child → children" or "mouse → mice".</remarks>
	public static string pluralString( this int count, string single )
	{
		if( 1 != count )
		{
			if( single[ single.Length - 1 ] != 's' )
				return $"{count} {single}s";
			return $"{count} {single}es";
		}
		return $"1 {single}";
	}

	public static bool isPartial( this INamedTypeSymbol symbol )
	{
		// A type can have multiple declarations (partial definitions)
		foreach( var decl in symbol.DeclaringSyntaxReferences )
		{
			var syntax = decl.GetSyntax() as TypeDeclarationSyntax;
			if( syntax?.Modifiers.Any( m => m.IsKind( SyntaxKind.PartialKeyword ) ) == true )
				return true;
		}
		return false;
	}

	public static string typeName( string name, INamespaceSymbol ns, ISymbol curr )
	{
		bool sameNamespace = SymbolEqualityComparer.Default.Equals( ns, curr.ContainingNamespace );
		if( !ns.IsGlobalNamespace && !sameNamespace )
			return $"{ns.str()}.{name}";
		else
			return name;
	}
}