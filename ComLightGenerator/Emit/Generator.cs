namespace ComLightGenerator.Emit;
using Microsoft.CodeAnalysis;
using System.Diagnostics;

sealed class Generator
{
	readonly string folder;
	readonly GeneratorMode mode;
	readonly MarshallerMethods defaultMarshaller;
	readonly Dictionary<INamedTypeSymbol, MarshallerMethods> marshallers = new( SymbolEqualityComparer.Default );

	public Generator( string folder, GeneratorMode mode )
	{
		this.folder = folder;
		this.mode = mode;
		Debug.Assert( Directory.Exists( folder ) );
		defaultMarshaller = new MarshallerMethods( null );
	}

	public void generate( in ComInterface ci, string visibility )
	{
		IfaceMeta iface = makeInterface( ci );

		using var source = new SourceWriter();
		GeneratorMode mode = source.header( iface, this.mode );

		using( var db = source.delegates( iface.iface ) )
		{
			foreach( ComMethod mi in iface.methods )
				db.addDelegate( mi );
		}

		if( iface.direction != eMarshalDirection.ToNative )
		{
			using( var p = source.proxy() )
			{
				p.addClass( iface );
				p.addConstructor( iface );
				foreach( ComMethod mi in iface.methods )
					p.addMethod( iface, mi );
			}
		}

		using( var m = source.marshaller( iface, visibility, mode ) )
			m.addClass();

		string path = Path.Combine( folder, $"{iface.name}.cl.cs" );
		if( File.Exists( path ) )
			throw new ArgumentException( $"Multiple COM interface with the same name \"{iface.name}\", this is not supported" );
		source.writeResult( path );
	}

	IfaceMeta makeInterface( in ComInterface ci )
	{
		MarshallerMethods marshaller = defaultMarshaller;
		if( null != ci.conventions )
		{
			if( marshallers.TryGetValue( ci.conventions, out var custom ) )
				marshaller = custom;
			else
			{
				custom = new MarshallerMethods( ci.conventions );
				marshaller = custom;
				marshallers.Add( ci.conventions, custom );
			}
		}

		return new IfaceMeta( ci, marshaller );
	}
}