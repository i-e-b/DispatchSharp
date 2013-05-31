namespace DispatchSharp
{
	public interface IWorkQueue<T>
	{
		/// <summary> Add an item to the queue </summary>
		void Enqueue(T work);

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
	}

	public interface IWorkQueueItem<T>
	{
		bool HasItem { get; }
		T Item { get; }

		void Finish();
		void Cancel();
	}
}