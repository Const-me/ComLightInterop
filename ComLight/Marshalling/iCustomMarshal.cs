using ComLight.Marshalling;
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace ComLight
{
	/// <summary>A custom marshaller</summary>
	public abstract class iCustomMarshal
	{
		/// <summary>Convert parameter type, the input is what's in C# interface, the output is what C++ will get.</summary>
		public abstract Type getNativeType( Type managed );

		/// <summary>Apply optional attributes to native delegate parameter.</summary>
		public virtual void applyDelegateParams( ParameterInfo source, ParameterBuilder destination ) { }

		/// <summary>Build expressions to convert .NET object to C++ value.
		public abstract Expressions native( ParameterExpression eManaged, bool isInput );

		/// <summary>Build an expression to convert from C++ value into .NET object.</summary>
		public abstract Expression managed( ParameterExpression eNative );
	}
}