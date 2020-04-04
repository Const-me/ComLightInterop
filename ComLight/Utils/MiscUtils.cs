using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ComLight
{
	static class MiscUtils
	{
		public static TValue lookup<TKey, TValue>( this Dictionary<TKey, TValue> dict, TKey key )
		{
			if( dict.TryGetValue( key, out TValue v ) )
				return v;
			return default( TValue );
		}

		public static bool isEmpty<T>( this T[] arr )
		{
			return null == arr || arr.Length <= 0;
		}

		public static bool notEmpty<T>( this T[] arr )
		{
			return arr != null && arr.Length > 0;
		}

		public static bool isEmpty<T>( this ICollection<T> list )
		{
			return null == list || list.Count <= 0;
		}

		public static bool notEmpty<T>( this ICollection<T> list )
		{
			return list != null && list.Count > 0;
		}

		/// <summary>ConstantExpression with boolean value `true`</summary>
		public static readonly ConstantExpression eTrue = Expression.Constant( true, typeof( bool ) );

		/// <summary>ConstantExpression with boolean value `false`</summary>
		public static readonly ConstantExpression eFalse = Expression.Constant( false, typeof( bool ) );

		/// <summary>'ref IntPtr' type</summary>
		public static readonly Type intPtrRef = typeof( IntPtr ).MakeByRefType();

		/// <summary>ConstantExpression with integer value S_OK</summary>
		public static readonly ConstantExpression S_OK = Expression.Constant( IUnknown.S_OK, typeof( int ) );

		/// <summary>ConstantExpression with integer value S_FALSE</summary>
		public static readonly ConstantExpression S_FALSE = Expression.Constant( IUnknown.S_FALSE, typeof( int ) );

		/// <summary>ConstantExpression with integer value E_UNEXPECTED</summary>
		public static readonly ConstantExpression E_UNEXPECTED = Expression.Constant( IUnknown.E_UNEXPECTED, typeof( int ) );

		/// <summary>ConstantExpression with IntPtr value IntPtr.Zero</summary>
		public static readonly ConstantExpression nullptr = Expression.Constant( IntPtr.Zero, typeof( IntPtr ) );

		public static Type[] noTypes = new Type[ 0 ];

		/// <summary>If the argument is `ref something`, return that something. Otherwise return the argument.</summary>
		public static Type unwrapRef( this Type tp )
		{
			if( !tp.IsByRef )
				return tp;
			return tp.GetElementType();
		}

		public static T getTarget<T>( this WeakReference<T> wr ) where T : class
		{
			T result;
			if( wr.TryGetTarget( out result ) )
				return result;
			return null;
		}

		public static bool isDead<T>( this WeakReference<T> wr ) where T : class
		{
			return !wr.TryGetTarget( out T unused );
		}
	}
}