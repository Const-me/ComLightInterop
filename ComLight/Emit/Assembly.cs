// Uncomment the following line if you want the dynamic assembly to be saved. Only works on desktop .NET framework.
// #define DBG_SAVE_DYNAMIC_ASSEMBLY
using System.Reflection;
using System.Reflection.Emit;
using System;

namespace ComLight.Emit
{
	/// <summary></summary>
	static class Assembly
	{
		public static readonly AssemblyBuilder assemblyBuilder;
		public static readonly ModuleBuilder moduleBuilder;

		static Assembly()
		{
			// Create dynamic assembly builder, and cache some reflected stuff we use to build these proxies in runtime.
			var an = new AssemblyName( "ComLight.Wrappers" );

#if NETCOREAPP || !DBG_SAVE_DYNAMIC_ASSEMBLY
			assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly( an, AssemblyBuilderAccess.Run );
			moduleBuilder = assemblyBuilder.DefineDynamicModule( "MainModule" );
#else
			AssemblyBuilderAccess aba = AssemblyBuilderAccess.RunAndSave;
			assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly( an, aba );
			moduleBuilder = assemblyBuilder.DefineDynamicModule( "MainModule", an.Name + ".dll" );

			Action actSave = () =>
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
			AppDomain.CurrentDomain.UnhandledException += ( object sender, UnhandledExceptionEventArgs e ) =>
			{
				actSave();
			};
			AppDomain.CurrentDomain.ProcessExit += ( object sender, EventArgs e ) =>
			{
				actSave();
			};
#endif
		}
	}
}