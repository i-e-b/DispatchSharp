namespace DispatchSharp
{
	/// <summary>
	/// Contract for a set of workers that can run actions against
	/// work queue items.
	/// </summary>
	/// <typeparam name="T">Type of items on the work queue</typeparam>
	public interface IWorkerPool<T>
	{
		/// <summary>
		/// Set source queue and managing dispatcher
		/// </summary>
		void SetSource(IDispatch<T> dispatch, IWorkQueue<T> queue);

		/// <summary>
		/// Start processing queue items as they become available
		/// </summary>
		void Start();

		/// <summary>
		/// Stop processing incoming queue items.
		/// Current work should be finished or cancelled before returning.
		/// </summary>
		void Stop();

		/// <summary>
		/// Current number of workers running actions against queue items
		/// </summary>
		int WorkersInflight();

		/// <summary>
		/// Returns number of worker threads in use
		/// </summary>
		int PoolSize();
	}
}