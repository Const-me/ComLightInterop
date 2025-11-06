namespace ComLightGenerator;

readonly struct CommandLine
{
	public readonly string inputProject;
	public readonly string visibility;
	readonly string generatedFolderName;

	public CommandLine( string[] args )
	{
		if( args == null || args.Length != 1 )
			throw new ArgumentException( "Usage: ComLightGenerator.exe SomeProject.csproj" );
		inputProject = args[ 0 ];
		if( !File.Exists( inputProject ) )
			throw new FileNotFoundException( "Input project is missing: " + inputProject );

		// If you need to customize these either add moar CLI arguments and parse better,
		// or simply change the strings below and build this tool.
		visibility = "internal";
		generatedFolderName = "ComLight";
	}

	public string generatedFolder()
	{
		string dir = Path.GetDirectoryName( inputProject )!;
		return Path.Combine( dir, generatedFolderName );
	}
}