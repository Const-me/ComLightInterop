namespace ComLightGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Runtime.CompilerServices;

internal class Program
{
	static async Task<List<ComInterface>> findInterfaces( MSBuildWorkspace workspace, string projectPath )
	{
		Project project = await workspace.OpenProjectAsync( projectPath );
		Compilation? compilation = await project.GetCompilationAsync();
		List<ComInterface> list = new();
		if( null != compilation )
			await Locator.findInterfaces( list, compilation );
		return list;
	}

	static async Task<int> Main( string[] args )
	{
		try
		{
			CommandLine commandLine = new CommandLine( args );

			using MSBuildWorkspace workspace = MSBuildWorkspace.Create();
			List<ComInterface> interfaces = await findInterfaces( workspace, commandLine.inputProject );

			if( interfaces.Count <= 0 )
			{
				Console.Error.WriteLine( "The provided project does not contain any COM interfaces" );
				return 0;
			}

			foreach( ComInterface i in interfaces )
				i.iface.validate();

			string dir = commandLine.generatedFolder();
			Directory.CreateDirectory( dir );
			foreach( string fi in Directory.EnumerateFiles( dir, "*.cs", SearchOption.TopDirectoryOnly ) )
				File.Delete( fi );

			var gen = new Emit.Generator( dir );
			foreach( ComInterface i in interfaces )
				gen.generate( i, commandLine.visibility );
			Console.Error.WriteLine( "Generated {0}", interfaces.Count.pluralString( "COM interface implementation" ) );
			return 0;
		}
		catch( Exception ex )
		{
			Console.Error.WriteLine( ex.Message );
			return ex.HResult;
		}
	}

	/*
	static string visibility => "internal";

	static string dbgFindProject( [CallerFilePath] string? thisSource = null )
	{
		string? dir = Path.GetDirectoryName( thisSource );
		dir = Path.GetDirectoryName( dir )!;
		// return Path.Combine( dir, "Demos", "HelloWorldCS", "HelloWorldCS.csproj" );
		return Path.Combine( dir, "ComLight", "ComLight.csproj" );
		// return Path.Combine( dir, "Demos", "StreamsCS", "StreamsCS.csproj" );
	}

	static string inputProject => dbgFindProject();

	const string generatedFolder = "ComLight"; 
	*/
}