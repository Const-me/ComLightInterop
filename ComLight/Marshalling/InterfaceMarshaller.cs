using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace ComLight.Marshalling
{
	/// <summary>Used automatically by the library, when COM objects create or consume other COM objects.</summary>
	/// <remarks>In some rare cases you might need to use it directly, <seealso cref="MarshallerAttribute" />.</remarks>
	public class InterfaceMarshaller<I>: iCustomMarshal where I : class
	{
		/// <summary>IntPtr for input parameters, or `ref IntPtr` for output parameters</summary>
		public override Type getNativeType( ParameterInfo managedParameter )
		{
			Type managed = managedParameter.ParameterType;
			if( managed == typeof( I ) )
				return typeof( IntPtr );
			if( managed == typeof( I ).MakeByRefType() )
			{
				if( managedParameter.IsIn )
					throw new ArgumentException( "COM interfaces can only be marshaled in or out, ref is not supported for them" );
				return MiscUtils.intPtrRef;
			}
			throw new ApplicationException( $"InterfaceMarshaller is used with a wrong parameter type { managed.FullName }" );
		}

		/// <summary>Add [Out] attribute if needed</summary>
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

		/// <summary>Expressions to convert native COM pointer into .NET object, for <see cref="eMarshalDirection.ToNative" /> marshaling direction.</summary>
		/// <remarks>For output parameters it does the opposite, wraps .NET object into new <see cref="ManagedObject" /> and calls AddRef.</remarks>
		public override Expressions managed( ParameterExpression eNative, bool isInput )
		{
			if( isInput )
				return Expressions.input( Expression.Call( miWrapNative, eNative ), null );

			var eManaged = Expression.Variable( typeof( I ) );
			var eWrap = Expression.Call( miWrapManaged, eManaged, MiscUtils.eTrue );
			var eResult = Expression.Assign( eNative, eWrap );
			return Expressions.output( eManaged, eResult );
		}

		/// <summary>Expressions to convert .NET object to native COM pointer, for <see cref="eMarshalDirection.ToManaged" /> marshaling direction.</summary>
		/// <remarks>For output parameters it does the opposite, wraps native pointer into new object.
		/// No AddRef is necessary. By convention, C++ COM methods which create or return objects already do that before they return them.</remarks>
		public override Expressions native( ParameterExpression eManaged, bool isInput )
		{
			if( isInput )
				return Expressions.input( Expression.Call( miWrapManaged, eManaged, MiscUtils.eFalse ), eManaged );

			var eNative = Expression.Variable( typeof( IntPtr ) );
			var eWrap = Expression.Call( miWrapNative, eNative );
			var eResult = Expression.Assign( eManaged, eWrap );
			return Expressions.output( eNative, eResult );
		}
	}
}