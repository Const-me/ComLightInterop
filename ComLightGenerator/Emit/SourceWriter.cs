namespace ComLightGenerator.Emit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
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

	public void header( INamedTypeSymbol iface )
	{
		w.WriteLine( "#nullable enable" );
		INamespaceSymbol ns = iface.ContainingNamespace;
		if( !ns.IsGlobalNamespace )
			w.WriteLine( "namespace {0};", ns.str() );
		w.WriteLine( "using System;" );
		w.WriteLine( "using System.Runtime.InteropServices;" );
		w.WriteLine( "using System.Runtime.InteropServices.Marshalling;" );
		w.WriteLine( "using ComLight;" );
		w.WriteLine();
	}

	public DelegatesBuilder delegates( INamedTypeSymbol iface )
	{
		string name = $"{iface.Name}_native";
		return new DelegatesBuilder( w, name );
	}

	public ProxyBuilder proxy() =>
		new ProxyBuilder( w );

	public Marshaller marshaller( in IfaceMeta iface, string visibility ) =>
		new Marshaller( w, iface, visibility );
}