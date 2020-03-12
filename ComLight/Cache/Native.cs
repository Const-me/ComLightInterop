using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using InterfacesMap = System.Runtime.CompilerServices.ConditionalWeakTable<ComLight.RuntimeClass, System.Type[]>;

namespace ComLight.Cache
{
	/// <summary>Tracks live COM objects implemented in C++.</summary>
	/// <remarks>Due to interfaces inheritance, the same IntPtr native pointer can be wrapped into multiple proxies. That's why we need a multimap here.</remarks>
	static class Native
	{
		static readonly object syncRoot = new object();

		/// <summary>Array of COM interface types implemented by RuntimeClass proxy. We now support interfaces inheritance, a proxy may implement more than one.</summary>
		/// <remarks>It's gonna be quite short anyway, most often just 1 interface, sometimes 2-3. That's why array instead of a hash map.</remarks>
		static Type[] collectComInterfaces( RuntimeClass rc )
		{
			return rc.GetType().GetInterfaces()
				.Where( i => i.hasCustomAttribute<ComInterfaceAttribute>() )
				.ToArray();
		}

		static readonly InterfacesMap.CreateValueCallback ifacesCallback = collectComInterfaces;

		static readonly Dictionary<IntPtr, InterfacesMap> native = new Dictionary<IntPtr, InterfacesMap>();

		public static void add( IntPtr p, RuntimeClass rc )
		{
			Debug.Assert( p != IntPtr.Zero );

			lock( syncRoot )
			{
				InterfacesMap map;
				if( !native.TryGetValue( p, out map ) )
				{
					map = new InterfacesMap();
					native.Add( p, map );
				}
				map.GetValue( rc, ifacesCallback );
			}
		}

		public static bool drop( IntPtr p, RuntimeClass rc )
		{
			lock( syncRoot )
			{
				if( native.TryGetValue( p, out InterfacesMap map ) )
				{
					bool removed = map.Remove( rc );
					if( !map.Any() )
						native.Remove( p );
					return removed;
				}
				return false;
			}
		}

		public static RuntimeClass lookup( IntPtr p, Type tInterface )
		{
			lock( syncRoot )
			{
				if( !native.TryGetValue( p, out InterfacesMap map ) )
					return null;

				foreach( var kvp in map )
				{
					if( !kvp.Key.isAlive() )
						continue;

					if( kvp.Value.Contains( tInterface ) )
						return kvp.Key;
				}
				return null;
			}
		}
	}
}