namespace DispatchSharp;

/// <summary>
/// Contract for polling queue sources
/// </summary>
/// <typeparam name="T">Type of item produced</typeparam>
public interface IPollSource<T>
{
	/// <summary>
	/// Try to get a new item
	/// </summary>
	/// <param name="item">item produced, or default</param>
	/// <returns>true if item produced, false if no item</returns>
	bool TryGet(out T? item);
}