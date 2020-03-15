using ComLight.Marshalling;
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace ComLight
{
	/// <summary>A custom marshaller.</summary>
	/// <remarks>If you're implementing this class, note that for performance reasons the library only creates a single instance of each type of marshaller, regardless on how many methods or interfaces are using it.</remarks>
	public abstract class iCustomMarshal
	{
		/// <summary>Convert parameter type, the input is what's in C# interface, the output is what C++ will get.</summary>
		public abstract Type getNativeType( ParameterInfo managedParameter );

		/// <summary>Apply optional attributes to native delegate parameter.</summary>
		public virtual void applyDelegateParams( ParameterInfo source, ParameterBuilder destination )
		{ }

		/// <summary>Expressions to convert .NET object to C++ value.</summary>
		public virtual Expressions native( ParameterExpression eManaged, bool isInput )
		{
			throw new NotSupportedException( $"The marshaller type { GetType().FullName } doesn't support C# to C++ direction" );
		}

		/// <summary>Expressions to convert from C++ value into .NET object.</summary>
		public virtual Expressions managed( ParameterExpression eNative, bool isInput )
		{
			throw new NotSupportedException( $"The marshaller type { GetType().FullName } doesn't support C++ to C# direction" );
		}
	}
}