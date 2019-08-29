using ComLight.Marshalling;
using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace ComLight.IO
{
	class WriteStreamMarshal: iCustomMarshal
	{
		public override Type getNativeType( Type managed )
		{
			if( managed == typeof( Stream ) )
				return typeof( IntPtr );
			if( managed == typeof( Stream ).MakeByRefType() )
				return typeof( IntPtr ).MakeByRefType();

			throw new ArgumentException( "[ReadStream] must be applied to parameters of type Stream" );
		}

		static readonly MethodInfo miWrapManaged;
		static readonly MethodInfo miWrapNative;

		static WriteStreamMarshal()
		{
			BindingFlags bf = BindingFlags.Public | BindingFlags.Static;
			miWrapManaged = typeof( NativeWriteStream ).GetMethod( "create", bf );
			miWrapNative = typeof( ManagedWriteStream ).GetMethod( "create", bf );
		}

		public override Expressions native( ParameterExpression eManaged, bool isInput )
		{
			if( isInput )
				return new Expressions( Expression.Call( miWrapManaged, eManaged ) );

			var eNative = Expression.Variable( typeof( IntPtr ) );
			var eWrap = Expression.Call( miWrapNative, eNative );
			var eResult = Expression.Assign( eManaged, eWrap );
			return new Expressions( eNative, eNative, eResult );
		}

		public override Expression managed( ParameterExpression eNative )
		{
			return Expression.Call( miWrapNative, eNative );
		}
	}
}