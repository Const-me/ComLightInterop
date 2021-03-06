﻿using System;
using System.Collections.Generic;

namespace ComLight
{
	class NativeWrapperCache<T> where T : class
	{
		readonly object syncRoot = new object();
		readonly Dictionary<IntPtr, WeakReference<T>> instances = new Dictionary<IntPtr, WeakReference<T>>();
		readonly Func<IntPtr, T> factory;

		public NativeWrapperCache( Func<IntPtr, T> f )
		{
			factory = f;
		}

		public T wrap( IntPtr nativeComPointer )
		{
			if( nativeComPointer == IntPtr.Zero )
				return null;

			WeakReference<T> wr;
			T result;
			lock( syncRoot )
			{
				if( instances.TryGetValue( nativeComPointer, out wr ) )
				{
					if( wr.TryGetTarget( out result ) )
						return result;
				}
				result = factory( nativeComPointer );
				if( null == wr )
				{
					wr = new WeakReference<T>( result );
					instances.Add( nativeComPointer, wr );
				}
				else
					wr.SetTarget( result );
				return result;
			}
		}

		public void dropIfDead( IntPtr p )
		{
			lock( syncRoot )
			{
				WeakReference<T> wr;
				if( !instances.TryGetValue( p, out wr ) )
					return;
				if( wr.isDead() )
					instances.Remove( p );
			}
		}
	}
}