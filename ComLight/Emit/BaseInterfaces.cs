using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ComLight.Emit
{
	class BaseInterfaces
	{
		readonly Type tInterface;
		readonly TypeBuilder typeBuilder;
		readonly Dictionary<string, MethodInfo[]> baseMethods;

		BaseInterfaces( TypeBuilder typeBuilder, Type tInterface, Type[] baseInterfaces )
		{
			this.tInterface = tInterface;
			this.typeBuilder = typeBuilder;
			baseMethods = baseInterfaces
				.SelectMany( i => i.GetMethods() )
				.GroupBy( m => m.Name )
				.ToDictionary( mg => mg.Key, mg => mg.ToArray() );
		}

		public static BaseInterfaces createIfNeeded( TypeBuilder typeBuilder, Type tInterface )
		{
			var bases = tInterface.GetInterfaces();
			if( bases.isEmpty() )
				return null;
			return new BaseInterfaces( typeBuilder, tInterface, bases );
		}

		public void implementedMethod( MethodBuilder newMethod, string name )
		{
			MethodInfo[] methods = baseMethods.lookup( name );
			if( methods.isEmpty() )
				return;
			foreach( var bm in methods )
				typeBuilder.DefineMethodOverride( newMethod, bm );
		}
	}
}