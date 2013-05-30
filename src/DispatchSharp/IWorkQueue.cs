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

		/// <summary> Returns when at least one item is available. Implementations are free to return immediately. </summary>
		void BlockUntilReady();
	}

	public interface IWorkQueueItem<T>
	{
		bool HasItem { get; }
		T Item { get; }

		void Finish();
		void Cancel();
	}
}