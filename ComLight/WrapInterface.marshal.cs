using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq.Expressions;
using System.Linq;
using System.Collections.Generic;

namespace ComLight
{
	static partial class WrapInterface
	{
		class CustomMarshallers
		{
			static Delegate compileMarshallerDelegate( iCustomMarshal cm, Type tManaged )
			{
				ParameterExpression eInput = Expression.Parameter( tManaged, "managed" );
				Expression eNative = cm.native( eInput );
				Type tNative = cm.getNativeType( tManaged );
				if( eNative.Type != tNative )
					throw new ArgumentException( $"{ cm.GetType().FullName }.native() expected to return an expression of type { tNative.FullName }, got { eNative.Type.FullName } instead" );

				Type tDelegate = typeof( Func<,> );
				tDelegate = tDelegate.MakeGenericType( tManaged, tNative );
				return Expression.Lambda( tDelegate, eNative, eInput ).Compile();
			}

			Delegate[] delegates;
			FieldBuilder[] fields;
			readonly Dictionary<(iCustomMarshal, Type), int> lookup = new Dictionary<(iCustomMarshal, Type), int>();

			/// <summary>Collect custom marshalers required to handle the COM interface method.</summary>
			public void addFromMethod( MethodInfo mi )
			{
				foreach( var pi in mi.GetParameters() )
				{
					var cm = pi.customMarshaller();
					if( null == cm )
						continue;
					Type tManaged = pi.ParameterType;
					if( lookup.ContainsKey( (cm, tManaged) ) )
						continue;
					int idx = lookup.Count;
					lookup[ (cm, tManaged) ] = idx;
				}
			}

			/// <summary>Compile marshalers into delegates, emit readonly fields of corresponding types.</summary>
			public void emitFields( TypeBuilder tb )
			{
				delegates = new Delegate[ lookup.Count ];
				foreach( var kvp in lookup )
					delegates[ kvp.Value ] = compileMarshallerDelegate( kvp.Key.Item1, kvp.Key.Item2 );

				fields = new FieldBuilder[ lookup.Count ];
				for( int i = 0; i < delegates.Length; i++ )
				{
					string fieldName = $"cm_{ i + 1 }";
					FieldBuilder fb = tb.DefineField( fieldName, delegates[ i ].GetType(), privateReadonly );
					fields[ i ] = fb;
				}
			}

			public int Count => delegates.Length;

			public FieldBuilder getField( int i ) => fields[ i ];

			public IEnumerable<Type> delegateTypes => delegates.Select( d => d.GetType() );

			// Sequence of ConstantExpression with compiled custom marshaller delegates
			public IEnumerable<ConstantExpression> extraCtorArgs()
			{
				return delegates.Select( d => Expression.Constant( d, d.GetType() ) );
			}

			public FieldBuilder lookupField( ParameterInfo pi )
			{
				var cm = pi.customMarshaller();
				if( null == cm )
					return null;
				int idx = lookup[ (cm, pi.ParameterType) ];
				return fields[ idx ];
			}
		}
	}
}