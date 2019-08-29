using System.Reflection;
using System.Reflection.Emit;
using System;

namespace ComLight.Emit
{
	static class Assembly
	{
		const bool dbgSaveGeneratedAssembly = true;

		public static readonly AssemblyBuilder assemblyBuilder;
		public static readonly ModuleBuilder moduleBuilder;

		static Assembly()
		{
			// Create dynamic assembly builder, and cache some reflected stuff we use to build these proxies in runtime.
			var an = new AssemblyName( "ComLight.Wrappers" );

#if NETCOREAPP
			assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly( an, AssemblyBuilderAccess.Run );
			moduleBuilder = assemblyBuilder.DefineDynamicModule( "MainModule" );
#else
			AssemblyBuilderAccess aba = dbgSaveGeneratedAssembly ? AssemblyBuilderAccess.RunAndSave : AssemblyBuilderAccess.Run;
			assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly( an, aba );
			moduleBuilder = assemblyBuilder.DefineDynamicModule( "MainModule", an.Name + ".dll" );

			if( dbgSaveGeneratedAssembly )
			{
				AppDomain.CurrentDomain.ProcessExit += ( object sender, EventArgs e ) =>
				{
					string name = an.Name + ".dll";
					try
					{
						assemblyBuilder.Save( name );
					}
					catch( Exception ex )
					{
						Console.WriteLine( "Error saving the assembly: {0}", ex.Message );
					}
				};
			}
#endif
		}
	}
}