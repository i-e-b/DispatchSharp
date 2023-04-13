using System.Collections.Generic;

namespace DispatchSharp
{
	/// <summary>
	/// Contract for an ordered queue of work to be acted upon.
	/// All implementations should be thread-safe.
	/// </summary>
	/// <typeparam name="T">Type of item to be stored</typeparam>
	public interface IWorkQueue<T>
	{
		/// <summary> Add an item to the queue </summary>
		/// <param name="work">The item to be processed</param>
		/// <param name="name">OPTIONAL: name of the work item.</param>
		void Enqueue(T work, string? name = null);

		/// <summary> Try and get an item from this queue. Success is encoded in the WQI result 'HasItem' </summary>
		IWorkQueueItem<T> TryDequeue();

		/// <summary> Approximate snapshot length </summary>
		int Length();

		/// <summary>
		/// Advisory method: block if the queue is waiting to be populated.
		/// Should return true when items are available.
		/// Implementations may return false if polling and no items are available.
		/// Implementations are free to return immediately.
		/// Implementations are free to return true even if no items are available.
		/// </summary>
		bool BlockUntilReady();

		/// <summary>
		/// Snapshot of named items waiting in the queue.
		/// This can affect performance, so should be used for either
		/// very short queues, or for failure diagnostics.
		/// </summary>
		/// <returns>List of named items. May be empty. May contain duplicates.</returns>
		IEnumerable<string> AllItemNames();
	}
}