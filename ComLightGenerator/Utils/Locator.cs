namespace ComLightGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Buffers;
using System.Text;

static class Locator
{
	public static async Task findInterfaces( List<ComInterface> list, Compilation compilation )
	{
		IEnumerable<SyntaxTree>? sources = compilation.SyntaxTrees;
		if( null == sources )
			return;

		foreach( SyntaxTree tree in sources )
		{
			if( !findMarker( tree ) )
				continue;

			SemanticModel model = compilation.GetSemanticModel( tree );
			SyntaxNode root = await tree.GetRootAsync();

			foreach( SyntaxNode node in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>() )
			{
				INamedTypeSymbol? symbol = model.GetDeclaredSymbol( node ) as INamedTypeSymbol;
				if( null == symbol )
					continue;
				if( symbol.TypeKind != TypeKind.Interface )
					continue;
				AttributeData? attr = symbol.findAttribute( AttributeNames.comInterface );
				if( null == attr )
					continue;

				var args = attr.ConstructorArguments;
				if( args.Length < 1 )
					continue;

				string? iidStr = args[ 0 ].Value as string;
				if( string.IsNullOrEmpty( iidStr ) )
					continue;
				if( !Guid.TryParse( iidStr, out Guid iid ) )
					continue;

				eMarshalDirection direction = eMarshalDirection.BothWays;
				if( attr.ConstructorArguments.Length >= 2 )
				{
					TypedConstant tc = attr.ConstructorArguments[ 1 ];
					direction = (eMarshalDirection)tc.Value!;
				}
				list.Add( new ComInterface( symbol, iid, direction ) );
			}
		}
	}

	static readonly byte[] marker = Encoding.ASCII.GetBytes( "ComInterface" );

	static bool findMarker( SyntaxTree tree )
	{
		string path = tree.FilePath;
		if( path.EndsWith( ".cl.cs", StringComparison.OrdinalIgnoreCase ) )
			return false;
		if( !File.Exists( path ) )
			return false;

		using var file = File.OpenRead( path );
		int length = (int)file.Length;

		var pool = ArrayPool<byte>.Shared;
		byte[] arr = pool.Rent( length );
		try
		{
			Span<byte> span = arr.AsSpan( 0, length );
			file.ReadExactly( span );
			return span.IndexOf( marker ) >= 0;
		}
		finally
		{
			pool.Return( arr );
		}
	}
}