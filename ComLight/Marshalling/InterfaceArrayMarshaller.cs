using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace ComLight.Marshalling
{
	class InterfaceArrayMarshaller<I>: iCustomMarshal where I : class
	{
		public override Type getNativeType( ParameterInfo managedParameter )
		{
			if( managedParameter.IsOut )
				throw new ApplicationException( @"InterfaceArrayMarshaller can only marshal them one way" );
			Type managed = managedParameter.ParameterType;
			if( managed == typeof( I[] ) )
				return typeof( IntPtr[] );
			throw new ApplicationException( @"InterfaceArrayMarshaller is used with a wrong parameter type" );
		}

		public override void applyDelegateParams( ParameterInfo source, ParameterBuilder destination )
		{
			destination.applyMarshalAsAttribute( UnmanagedType.LPArray );
			destination.applyInAttribute();
		}

		static IntPtr[] wrapManaged( I[] managed, bool callAddRef )
		{
			if( null != managed )
			{
				// Not sure about the performance.
				// Theoretically, these small arrays should never leave generation 0 of the managed heap.
				// Practically, using `new` with GPU calls can create measurable load on GC.
				// Maybe we need to do something more sophisticated here, like use ArrayPool<IntPtr>.Shared for these arrays.
				IntPtr[] result = new IntPtr[ managed.Length ];
				for( int i = 0; i < managed.Length; i++ )
				{
					I obj = managed[ i ];
					if( null != obj )
						result[ i ] = ManagedWrapper.wrap<I>( obj, callAddRef );
				}
				return result;
			}
			return null;
		}

		/// <summary><see cref="ManagedWrapper.wrap{I}(object, bool)" /></summary>
		static readonly MethodInfo miWrapManaged = typeof( InterfaceArrayMarshaller<I> )
			.GetMethod( "wrapManaged", BindingFlags.Static | BindingFlags.NonPublic );

		public override Expressions managed( ParameterExpression eNative, bool isInput )
		{
			throw new NotImplementedException();
		}

		public override Expressions native( ParameterExpression eManaged, bool isInput )
		{
			if( isInput )
				return Expressions.input( Expression.Call( miWrapManaged, eManaged, MiscUtils.eFalse ) );

			throw new NotImplementedException();
		}
	}
}