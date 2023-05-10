using System.Collections.Generic;

namespace DispatchSharp;

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
	QueueState BlockUntilReady();

	/// <summary>
	/// Snapshot of named items waiting in the queue.
	/// This can affect performance, so should be used for either
	/// very short queues, or for failure diagnostics.
	/// </summary>
	/// <returns>List of named items. May be empty. May contain duplicates.</returns>
	IEnumerable<string> AllItemNames();
		
	/// <summary>
	/// Set a custom sleeper for any waits that happen on this dispatch.
	/// There is a default sleeper that provides a limited linear back-off.
	/// </summary>
	void SetSleeper(IBackOffWaiter sleeper);
}

/// <summary>
/// Tri-state for queue. Used for waiting and shutdown state
/// </summary>
public enum QueueState
{
	/// <summary>
	/// Queue population is not known.
	/// This is the normal state for polling jobs
	/// </summary>
	Unknown = 0,
	
	/// <summary>
	/// Queue has at least one item waiting to be processed
	/// </summary>
	HasItems = 1,
	
	/// <summary>
	/// Queue has no items waiting to be processed
	/// </summary>
	Empty = 2
}