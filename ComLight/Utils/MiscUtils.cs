using System.Collections.Generic;

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
	}
}