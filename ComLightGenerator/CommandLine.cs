namespace ComLightGenerator;

[Flags]
enum GeneratorMode: byte
{
	Net8 = 0,
	Framework = 1,
	Internal = 2,

	Net8Internal = Net8 | Internal,
	FrameworkInternal = Framework | Internal,
}

readonly struct CommandLine
{
	public readonly string[] inputProjects;
	public readonly string visibility;
	public readonly GeneratorMode mode;
	const string generatedFolderName = "ComLight";

	public CommandLine( string[] args )
	{
		List<string> projects = new List<string>();
		visibility = "internal";

		foreach( string str in args )
		{
			if( string.IsNullOrWhiteSpace( str ) )
				continue;
			string a = str.Trim();
			if( a[ 0 ] != '-' )
			{
				string path = a;
				if( !File.Exists( path ) )
					throw new FileNotFoundException( "Input project is missing: " + path );
				projects.Add( path );
				continue;
			}
			switch( str.ToLowerInvariant() )
			{
				case "-framework":
					mode |= GeneratorMode.Framework;
					break;
				case "-internal":
					mode |= GeneratorMode.Internal;
					break;
				case "-public":
					visibility = "public";
					break;
				default:
					throw new ArgumentException( $"Unknown command line parameter \"{str}\"" );
			}
		}

		if( !projects.Any() )
			throw new ArgumentException( "Usage: ComLightGenerator.exe SomeProject.csproj" );
		inputProjects = projects.ToArray();
	}

	public string generatedFolder( string csproj )
	{
		string dir = Path.GetDirectoryName( csproj )!;
		return Path.Combine( dir, generatedFolderName );
	}
}