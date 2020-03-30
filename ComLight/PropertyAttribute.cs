using System;

namespace ComLight
{
	/// <summary>Apply this attribute to properties if you want to customize the mapping between names of property and getter/setter methods</summary>
	[AttributeUsage( AttributeTargets.Property )]
	public class PropertyAttribute: Attribute
	{
		internal readonly string getterMethod;
		internal readonly string setterMethod;

		/// <summary>Specify custom name.</summary>
		public PropertyAttribute( string name )
		{
			getterMethod = "get" + name;
			setterMethod = "set" + name;
		}

		/// <summary>Specify custom names for both getter and setter.</summary>
		public PropertyAttribute( string getter, string setter )
		{
			getterMethod = getter;
			setterMethod = setter;
		}
	}
}