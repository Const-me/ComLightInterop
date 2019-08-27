﻿using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace ComLight.IO
{
	class WriteStreamMarshal: iCustomMarshal
	{
		void iCustomMarshal.applyDelegateParams( ParameterInfo source, ParameterBuilder destination )
		{
			// Nothing to do here
		}

		Type iCustomMarshal.getNativeType( Type managed )
		{
			if( managed != typeof( Stream ) )
				throw new ArgumentException( "[ReadStream] must be applied to parameters of type Stream" );
			return typeof( IntPtr );
		}

		static readonly MethodInfo miWrapManaged;
		static readonly MethodInfo miWrapNative;

		static WriteStreamMarshal()
		{
			BindingFlags bf = BindingFlags.Public | BindingFlags.Static;
			miWrapManaged = typeof( NativeWriteStream ).GetMethod( "create", bf );
			miWrapNative = typeof( ManagedWriteStream ).GetMethod( "create", bf );
		}

		Expression iCustomMarshal.native( ParameterExpression eManaged )
		{
			return Expression.Call( miWrapManaged, eManaged );
		}

		Expression iCustomMarshal.managed( ParameterExpression eNative )
		{
			return Expression.Call( miWrapNative, eNative );
		}
	}
}