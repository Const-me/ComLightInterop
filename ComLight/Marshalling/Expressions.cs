using System.Linq.Expressions;

namespace ComLight.Marshalling
{
	/// <summary>Custom marshaling expressions. They are compiled into IL code while creating instances of proxies.</summary>
	public class Expressions
	{
		/// <summary>Local variables, can be null</summary>
		public readonly ParameterExpression[] variables = null;

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
			variables = new ParameterExpression[ 1 ] { var1 };
			this.argument = argument;
			this.after = after;
		}

		/// <summary>Simple marshalling expression for `in` direction</summary>
		public static Expressions input( Expression arg )
		{
			return new Expressions( arg );
		}

		/// <summary>Marshalling expression for `out` direction: declare a local variable, pass it to the delegate, then assign the result output parameter by wrapping the local variable using a custom expression.</summary>
		public static Expressions output( ParameterExpression var, Expression after )
		{
			return new Expressions( var, var, after );
		}
	}
}