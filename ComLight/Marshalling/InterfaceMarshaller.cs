using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace ComLight.Marshalling
{
	class InterfaceMarshaller<I>: iCustomMarshal where I : class
	{
		public override Type getNativeType( ParameterInfo managedParameter )
		{
			Type managed = managedParameter.ParameterType;
			if( managed == typeof( I ) )
				return typeof( IntPtr );
			if( managed == typeof( I ).MakeByRefType() )
			{
				if( managedParameter.IsIn )
					throw new ArgumentException( "COM interfaces can only be marshalled in or out, ref is not supported for them" );
				return MiscUtils.intPtrRef;
			}
			throw new ApplicationException( @"InterfaceMarshaller is used with a wrong parameter type" );
		}

		public override void applyDelegateParams( ParameterInfo source, ParameterBuilder destination )
		{
			if( source.IsOut )
				destination.applyOutAttribute();
		}

		/// <summary><see cref="ManagedWrapper.wrap{I}(object, bool)" /></summary>
		static readonly MethodInfo miWrapManaged = typeof( ManagedWrapper )
			.GetMethod( "wrap" )
			.MakeGenericMethod( typeof( I ) );

		/// <summary><see cref="NativeWrapper.wrap{I}(IntPtr)" /></summary>
		static readonly MethodInfo miWrapNative = typeof( NativeWrapper )
			.GetMethod( "wrap", new Type[ 1 ] { typeof( IntPtr ) } )
			.MakeGenericMethod( typeof( I ) );

		public override Expressions managed( ParameterExpression eNative, bool isInput )
		{
			if( isInput )
				return Expressions.input( Expression.Call( miWrapNative, eNative ) );

			var eManaged = Expression.Variable( typeof( I ) );
			var eWrap = Expression.Call( miWrapManaged, eManaged, MiscUtils.eTrue );
			var eResult = Expression.Assign( eNative, eWrap );
			return Expressions.output( eManaged, eResult );
		}

		public override Expressions native( ParameterExpression eManaged, bool isInput )
		{
			if( isInput )
				return Expressions.input( Expression.Call( miWrapManaged, eManaged, MiscUtils.eFalse ) );

			var eNative = Expression.Variable( typeof( IntPtr ) );
			var eWrap = Expression.Call( miWrapNative, eNative );
			var eResult = Expression.Assign( eManaged, eWrap );
			return Expressions.output( eNative, eResult );
		}
	}
}