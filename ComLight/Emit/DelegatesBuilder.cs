using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ComLight.Emit
{
	/// <summary>A wrapper around <see cref="TypeBuilder" /> which assigns unique names to the delegates.</summary>
	/// <remarks>Both C# and C++ allow interface methods to have the same name, distinguished by different argument types.</remarks>
	sealed class DelegatesBuilder
	{
		public readonly TypeBuilder typeBuilder;
		readonly HashSet<string> typeNames = new HashSet<string>();

		public DelegatesBuilder( string name )
		{
			typeBuilder = Assembly.moduleBuilder.emitStaticClass( name );
		}

		string assignName( MethodInfo comMethod )
		{
			if( typeNames.Add( comMethod.Name ) )
				return comMethod.Name;
			string alt = comMethod.Name + comMethod.GetHashCode().ToString( "x" );
			if( typeNames.Add( alt ) )
				return alt;
			throw new ApplicationException( $"NativeDelegatesBuilder unable to assign unique name for a method { comMethod.DeclaringType.FullName }.{ comMethod.Name }" );
		}

		public TypeBuilder defineMulticastDelegate( MethodInfo comMethod )
		{
			string name = assignName( comMethod );
			TypeAttributes ta = TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.NestedPublic;
			return typeBuilder.DefineNestedType( name, ta, typeof( MulticastDelegate ) );
		}

		/// <summary>Creates a System.Type object for the class. After defining fields and methods on the class, CreateType is called in order to load its Type object.</summary>
		public Type createType()
		{
			return typeBuilder.CreateType();
		}
	}
}