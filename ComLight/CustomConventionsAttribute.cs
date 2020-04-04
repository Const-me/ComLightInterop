using System;
using System.Reflection;

namespace ComLight
{
	/// <summary>Apply to COM interface to use custom prologue function, and custom errors marshaling.</summary>
	/// <remarks>Protip: C++ doesn’t feature async-await. You can keep per-thread error context in a static variable marked with [ThreadStatic] attribute.</remarks>
	[AttributeUsage( AttributeTargets.Interface, Inherited = false )]
	public class CustomConventionsAttribute: Attribute
	{
		/// <summary>This static method will be called immediately before every native call.</summary>
		public readonly MethodInfo prologue = null;

		/// <summary>This static method will be used to convert HRESULT codes into .NET exceptions. It should throw appropriate exceptions for FAILED codes, and do nothing if the code is SUCCEEDED.</summary>
		public readonly MethodInfo throwException = null;

		/// <summary>This static method will be used to convert HRESULT codes for COM methods which return booleans. It should throw exceptions for FAILED codes, return true for S_OK, return false for anything else.</summary>
		public readonly MethodInfo throwAndReturnBool = null;

		/// <summary>Construct with the type implementing the conventions.</summary>
		public CustomConventionsAttribute( Type type )
		{
			if( !type.IsPublic )
				throw new ArgumentException( $"The type { type.FullName } specified in [ CustomConventions ] attribute ain’t public." );

			var mi = type.GetMethod( "prologue", BindingFlags.Public | BindingFlags.Static, null, MiscUtils.noTypes, null );
			if( null != mi )
			{
				if( mi.ReturnType != typeof( void ) )
					throw new ApplicationException( $"The { type.FullName }.prologue() method returns something, it must be void." );
				prologue = mi;
			}

			Type[] it = new Type[ 1 ] { typeof( int ) };
			mi = type.GetMethod( "throwForHR", BindingFlags.Public | BindingFlags.Static, null, it, null );
			if( null != mi )
			{
				if( mi.ReturnType != typeof( void ) )
					throw new ApplicationException( $"The { type.FullName }.throwForHR() method returns something, it must be void." );
				throwException = mi;
			}

			mi = type.GetMethod( "throwAndReturnBool", BindingFlags.Public | BindingFlags.Static, null, it, null );
			if( null != mi )
			{
				if( mi.ReturnType != typeof( bool ) )
					throw new ApplicationException( $"The { type.FullName }.throwAndReturnBool() method must return bool." );
				throwAndReturnBool = mi;
			}
		}
	}
}