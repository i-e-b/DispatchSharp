using System;
using System.Threading;

namespace DispatchSharp;

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
	/// <param name="cantStopWarning">If provided, this method will be called for any
	/// thread that could not be stopped. Current versions of .Net do not give a
	/// way to stop threads unconditionally.</param>
	void Stop(Action<Thread>? cantStopWarning = null);

	/// <summary>
	/// Current number of workers running actions against queue items
	/// </summary>
	int WorkersInflight();

	/// <summary>
	/// Returns number of worker threads in use
	/// </summary>
	int PoolSize();
}