namespace DispatchSharp
{
	/// <summary>
	/// Contract for a work queue item that has been dequeued
	/// </summary>
	/// <typeparam name="T">Type of contained item</typeparam>
	public interface IWorkQueueItem<T>
	{
		/// <summary>
		/// Has an item been dequeued?
		/// If false, Item will be default value (i.e. null)
		/// </summary>
		bool HasItem { get; }

		/// <summary>
		/// Queue item if one was available
		/// </summary>
		T Item { get; }

		/// <summary>
		/// Call this to permanently remove an item from the queue
		/// </summary>
		void Finish();

		/// <summary>
		/// Call this to cancel the dequeue and return item to work queue.
		/// There is no guarantee where the item will be returned (head, end or somewhere in the middle)
		/// </summary>
		void Cancel();
	}
}