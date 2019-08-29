using ComLight.Marshalling;
using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace ComLight.IO
{
	class ReadStreamMarshal: iCustomMarshal
	{
		public override Type getNativeType( ParameterInfo managedParameter )
		{
			Type managed = managedParameter.ParameterType;
			if( managed == typeof( Stream ) )
				return typeof( IntPtr );
			if( managed == typeof( Stream ).MakeByRefType() )
			{
				if( managedParameter.IsIn )
					throw new ArgumentException( "[ReadStream] doesn't support ref parameters" );
				return typeof( IntPtr ).MakeByRefType();
			}
			throw new ArgumentException( "[ReadStream] must be applied to a parameter of type Stream" );
		}

		static readonly MethodInfo miWrapManaged;
		static readonly MethodInfo miWrapNative;

		static ReadStreamMarshal()
		{
			BindingFlags bf = BindingFlags.Public | BindingFlags.Static;
			miWrapManaged = typeof( NativeReadStream ).GetMethod( "create", bf );
			miWrapNative = typeof( ManagedReadStream ).GetMethod( "create", bf );
		}

		public override Expressions native( ParameterExpression eManaged, bool isInput )
		{
			if( isInput )
				return new Expressions( Expression.Call( miWrapManaged, eManaged, MiscUtils.eFalse ) );

			var eNative = Expression.Variable( typeof( IntPtr ) );
			var eWrap = Expression.Call( miWrapNative, eNative );
			var eResult = Expression.Assign( eManaged, eWrap );
			return new Expressions( eNative, eNative, eResult );
		}

		public override Expressions managed( ParameterExpression eNative, bool isInput )
		{
			if( isInput )
				return new Expressions( Expression.Call( miWrapNative, eNative ) );

			var eManaged = Expression.Variable( typeof( Stream ) );
			var eWrap = Expression.Call( miWrapManaged, eManaged, MiscUtils.eTrue );
			var eResult = Expression.Assign( eNative, eWrap );
			return new Expressions( eManaged, eManaged, eResult );
		}
	}
}