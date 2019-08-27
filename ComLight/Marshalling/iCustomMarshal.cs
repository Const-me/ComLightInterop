using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace ComLight
{
	/// <summary>A custom marshaller</summary>
	public interface iCustomMarshal
	{
		/// <summary>Convert parameter type, the input is what's in C# interface, the output is what C++ will get.</summary>
		Type getNativeType( Type managed );

		/// <summary>Apply optional attributes to native delegate parameter.</summary>
		void applyDelegateParams( ParameterInfo source, ParameterBuilder destination );

		/// <summary>Build an expression to convert from .NET object to C++ value.</summary>
		Expression native( ParameterExpression eManaged );

		/// <summary>Build an expression to convert from C++ value into .NET object.</summary>
		Expression managed( ParameterExpression eNative );
	}
}