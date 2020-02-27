namespace ComLight
{
	/// <summary>Implement this interface to get notified when C++ code releases the last native reference to your object.</summary>
	public interface iComDisposable
	{
		/// <summary>Called when C++ code calls IUnknown.Release(), and the count of references to this object from native code reaches zero.</summary>
		/// <remarks>This is a good place to dispose resources consumed by C++ code.</remarks>
		void lastNativeReferenceReleased();
	}
}