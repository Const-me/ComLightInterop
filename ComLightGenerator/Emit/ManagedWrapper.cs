namespace ComLightGenerator.Emit;
using Microsoft.CodeAnalysis;

static class ManagedWrapper
{
	public static void wrapManaged( StreamWriter w, in IfaceMeta iface )
	{
		int length = iface.methods.Length;
		for( int i = 0; i < length; i++ )
			makeLocalVar( w, iface, iface.methods[ i ] );

		w.Write( "		return [" );
		for( int i = 0; i < length; i++ )
		{
			if( i != 0 )
				w.Write( ", " );
			else
				w.Write( ' ' );
			w.Write( iface.methods[ i ].name );
		}
		if( length > 0 )
			w.Write( ' ' );
		w.WriteLine( "];" );
	}

	static void makeLocalVar( StreamWriter w, in IfaceMeta iface, in ComMethod mi )
	{
		w.Write( "		{0}_native.{1} {1} = delegate( nint _", iface.name, mi.name );

		ComParameter[] arr = mi.parameters;
		int retValIndex = -1;
		if( mi.retValIndex != null )
			retValIndex = mi.retValIndex.Value;

		for( int i = 0; i < arr.Length; i++ )
		{
			if( i == retValIndex )
			{
				retValIndex = -1;
				string argType = DelegatesBuilder.nativeRetValArgType( mi );
				w.Write( ", {0} _RetVal", argType );
				i--;
				continue;
			}
			w.Write( ", {0} {1}", arr[ i ].nativeArgType(), arr[ i ].name );
		}
		if( retValIndex >= 0 )
		{
			string argType = DelegatesBuilder.nativeRetValArgType( mi );
			w.Write( ", {0} _RetVal", argType );
		}
		w.WriteLine( " )" );
		w.WriteLine( "		{" );
		bool rawReturn = mi.returns.rawReturnType();
		string indent;
		if( !rawReturn )
		{
			w.WriteLine( "			try" );
			w.WriteLine( "			{" );
			indent = "\t\t\t\t";
		}
		else
			indent = "\t\t\t";

		bool anyCustomOutputs = mi.parameters.Any( cp => cp.marshalUsing != null && cp.isOutput );

		switch( mi.returns )
		{
			case eMethodReturn.Void:
				w.Write( indent );
				break;
			case eMethodReturn.Bool:
			case eMethodReturn.Int:
			case eMethodReturn.Pointer:
				if( !anyCustomOutputs )
					w.Write( "{0}return ", indent );
				else
					w.Write( "{0}var _RetVal = ", indent );
				break;
			case eMethodReturn.Value:
				w.Write( "{0}_RetVal = ", indent );
				break;
			case eMethodReturn.Object:
				w.Write( "{0}_RetVal = ", indent );
				w.Write( "{0}.AddRef.ConvertToUnmanaged( ", mi.retValMarshaller );
				break;
			default:
				throw new NotImplementedException();
		}

		w.Write( "obj.{0}(", mi.name );
		for( int i = 0; i < arr.Length; i++ )
		{
			if( i == 0 )
				w.Write( ' ' );
			else
				w.Write( ", " );

			string? marshal = arr[ i ].marshalUsing;
			if( null == marshal )
			{
				w.Write( arr[ i ].managedArgumentModifier() );
				w.Write( arr[ i ].name );
				continue;
			}
			if( arr[ i ].isOutput )
			{
				w.Write( "out var _{0}", arr[ i ].name );
				continue;
			}
			w.Write( "{0}( {1} )", arr[ i ].managedInputMarshaller(), arr[ i ].name );
		}
		if( arr.Length > 0 )
			w.Write( ' ' );
		w.Write( ')' );
		if( mi.returns == eMethodReturn.Bool && !anyCustomOutputs )
			w.Write( " ? 0 : 1" );
		else if( mi.returns == eMethodReturn.Object )
			w.Write( " )" );
		w.WriteLine( ";" );

		if( anyCustomOutputs )
		{
			foreach( var pi in arr )
			{
				if( null == pi.marshalUsing )
					continue;
				if( !pi.isOutput )
					continue;
				w.WriteLine( "{0}{1} = {2}( _{1} );", indent, pi.name, pi.managedOutputMarshaller() );
			}
		}
		switch( mi.returns )
		{
			case eMethodReturn.Void:
			case eMethodReturn.Object:
				w.WriteLine( "{0}return 0;", indent );
				break;
			case eMethodReturn.Bool:
				if( anyCustomOutputs )
					w.WriteLine( "{0}return _RetVal ? 0 : 1;", indent );
				break;
			case eMethodReturn.Int:
			case eMethodReturn.Pointer:
				if( anyCustomOutputs )
					w.WriteLine( "{0}return _RetVal;", indent );
				break;
		}

		if( !rawReturn )
		{
			w.WriteLine( "			}" );
			w.WriteLine( "			catch( Exception ex )" );
			w.WriteLine( "			{" );
			if( null != iface.marshaller.managedException )
				w.WriteLine( "				{0}( ex );", iface.marshaller.managedException );
			foreach( var pi in arr )
			{
				if( pi.symbol.RefKind != RefKind.Out )
					continue;
				w.WriteLine( "				{0} = default;", pi.name );
			}
			if( mi.retValIndex.HasValue )
				w.WriteLine( "				_RetVal = default;" );
			w.WriteLine( "				return ex.HResult;" );
			w.WriteLine( "			}" );
		}

		w.WriteLine( "		};" );
	}
}