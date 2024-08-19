using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ComLight.Marshalling
{
	/// <summary>Custom marshaling expressions. They are compiled into IL code while creating instances of proxies.</summary>
	public class Expressions
	{
		/// <summary>Local variables, can be null</summary>
		public readonly ParameterExpression variable = null;

		/// <summary>Expression to pass to the native function</summary>
		public readonly Expression argument;

		/// <summary>Called after the native function completes successfully. Can be null.</summary>
		public readonly Expression after = null;

		/// <summary>Construct with just the argument</summary>
		Expressions( Expression arg )
		{
			argument = arg;
		}

		/// <summary>Construct with local variables, argument expression, and post expression.</summary>
		public Expressions( ParameterExpression var1, Expression argument, Expression after )
		{
			variable = var1;
			this.argument = argument;
			this.after = after;
		}

		static readonly MethodInfo miKeepAlive = typeof( GC )
			.GetMethod( nameof( GC.KeepAlive ), BindingFlags.Static | BindingFlags.Public );

		/// <summary>Simple marshaling expression for `in` direction</summary>
		/// <remarks>Since version 1.3.9, this protects input object from GC for the duration of the C++ call.</remarks>
		public static Expressions input( Expression arg, Expression keepAlive )
		{
			if( null != keepAlive )
			{
				Expression after = Expression.Call( miKeepAlive, keepAlive );
				return new Expressions( null, arg, after );
			}
			else
				return new Expressions( arg );
		}

		/// <summary>Marshalling expression for `out` direction: declare a local variable, pass it to the delegate, then assign the result output parameter by wrapping the local variable using a custom expression.</summary>
		public static Expressions output( ParameterExpression var, Expression after )
		{
			return new Expressions( var, var, after );
		}
	}
}