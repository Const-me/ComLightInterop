﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ComLight.Emit
{
	sealed class BaseInterfaces
	{
		readonly Type tInterface;
		readonly TypeBuilder typeBuilder;
		readonly Dictionary<string, MethodInfo[]> baseMethods;

		BaseInterfaces( TypeBuilder typeBuilder, Type tInterface, IEnumerable<Type> baseInterfaces )
		{
			this.tInterface = tInterface;
			this.typeBuilder = typeBuilder;
			baseMethods = baseInterfaces
				.SelectMany( i => i.getMethodsWithoutProperties() )
				.GroupBy( m => m.Name )
				.ToDictionary( mg => mg.Key, mg => mg.ToArray() );
		}

		public static BaseInterfaces createIfNeeded( TypeBuilder typeBuilder, Type tInterface )
		{
			var bases = tInterface.GetInterfaces();
			if( bases.isEmpty() )
				return null;
			IEnumerable<Type> excludeDisposable = bases.Where( t => t != typeof( IDisposable ) );
			if( excludeDisposable.Any() )
				return new BaseInterfaces( typeBuilder, tInterface, excludeDisposable );
			return null;
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