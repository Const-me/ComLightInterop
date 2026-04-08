This `OfflineCodegen` branch implements newer version of the same library
specificaly targered towards .NET 8 and .NET 10 AOT (Ahead Of Time compiled).

Since I have implemented this initial version, I’ve been using it extensively 
to interop between AOT compiled C# and C++, also between AOT compiled .NET 10 and JIT compiled .NET Framework 4.8.

As you probably aware, ahead of time C# compiler doesn't support either reflection or runtime code generation.\
And the version of the library on the master branch is entirely based on these two parts of the runtime.

This branch implements a workaround by generating all necessary boilerplate codes in design time.

To compile the code generator, open ComLightAot.sln in Visual Studio, build ComLightGenerator project.

Usage examples:
```
ComLightGenerator.exe SomeProject.csproj
ComLightGenerator.exe ProjA.csproj ProjB.csproj
ComLightGenerator.exe Project.csproj -framework
ComLightGenerator.exe Project.csproj -internal
```
As you see, you have to specify one or more input C# projects, and optional switches.

All generated codes are placed in `ComLight` subfolder of the source project, creating it if necessary.

