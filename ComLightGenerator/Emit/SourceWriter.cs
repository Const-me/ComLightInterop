namespace ComLightGenerator.Emit;
using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Text;

sealed class SourceWriter: IDisposable
{
	MemoryStream? stream;
	readonly StreamWriter w;

	public SourceWriter()
	{
		stream = new MemoryStream();
		w = new StreamWriter( stream, Encoding.UTF8, 1024, true );
	}

	public void writeResult( string path )
	{
		w.Flush();
		w.Dispose();
		stream!.Seek( 0, SeekOrigin.Begin );

		using var dest = File.Create( path );
		stream.CopyTo( dest );
		stream.Dispose();
	}

	public void Dispose()
	{
		w.Dispose();
		stream?.Dispose();
	}

	public GeneratorMode header( in IfaceMeta meta, GeneratorMode mode )
	{
		w.WriteLine( "#pragma warning disable CS8981\t// The type name only contains lower-cased ascii characters" );
		w.WriteLine( "#pragma warning disable CS8603\t// Possible null reference return" );
		w.WriteLine( "#pragma warning disable CS8604\t// Possible null reference argument" );
		w.WriteLine( "#pragma warning disable CS8601\t// Possible null reference assignment" );

		w.WriteLine( "#nullable enable" );
		INamespaceSymbol ns = meta.iface.ContainingNamespace;
		if( !ns.IsGlobalNamespace )
			w.WriteLine( "namespace {0};", ns.str() );
		w.WriteLine( "using System;" );
		w.WriteLine( "using System.Runtime.InteropServices;" );

		switch( mode )
		{
			case GeneratorMode.Net8:
				if( !meta.isPartial )
					mode = GeneratorMode.Net8Internal;
				break;
			case GeneratorMode.Framework:
				if( !meta.classFactory )
					mode = GeneratorMode.FrameworkInternal;
				break;
		}

		if( mode == GeneratorMode.Net8 )
			w.WriteLine( "using System.Runtime.InteropServices.Marshalling;" );
		if( meta.iface.ContainingNamespace.str() != "ComLight" )
			w.WriteLine( "using ComLight;" );
		w.WriteLine();
		return mode;
	}

	public DelegatesBuilder delegates( INamedTypeSymbol iface )
	{
		string name = $"{iface.Name}_native";
		return new DelegatesBuilder( w, name );
	}

	public ProxyBuilder proxy() =>
		new ProxyBuilder( w );

	public Marshaller marshaller( in IfaceMeta iface, string visibility, GeneratorMode mode ) =>
		new Marshaller( w, iface, visibility, mode );
}