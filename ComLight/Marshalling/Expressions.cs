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

		public Expressions( Expression arg )
		{
			argument = arg;
		}

		public Expressions( ParameterExpression var1, Expression argument, Expression after )
		{
			variables = new ParameterExpression[ 1 ] { var1 };
			this.argument = argument;
			this.after = after;
		}
	}
}