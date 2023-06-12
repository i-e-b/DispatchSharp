using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DispatchSharp.Internal;

namespace DispatchSharp;

/// <summary>
/// Default dispatcher
/// </summary>
/// <typeparam name="T">Type of work item to be processed</typeparam>
public partial class Dispatch<T> : IDispatch<T>, IDispatchInternal<T>
{
	readonly IWorkQueue<T> _queue;
	readonly IWorkerPool<T> _pool;
	readonly Dictionary<Guid, Action<T>> _workActions;
	readonly object _lockObject;
		
	/// <summary>
	/// Internal inflight limit.
	/// </summary>
	protected int _inflightLimit;

	/// <summary>
	/// Create a dispatcher with a specific queue and worker pool
	/// </summary>
	public Dispatch(IWorkQueue<T> workQueue, IWorkerPool<T> workerPool)
	{
		_inflightLimit = Default.ThreadCount;

		Exceptions = (_, _) => { };
		_queue = workQueue;
		_pool = workerPool;

		_lockObject = new object();
		_workActions = new Dictionary<Guid, Action<T>>();

		_pool.SetSource(this, _queue);
	}

	/// <summary> Maximum number of work items being processed at any one time </summary>
	public int MaximumInflight()
	{
		return _inflightLimit;
	}

	/// <summary>
	/// Maximum number of work items being processed at any one time
	/// </summary>
	public void SetMaximumInflight(int max) {
		_inflightLimit = max;
	}

	/// <summary> Snapshot of number of work items being processed </summary>
	public int CurrentInflight()
	{
		return _pool.WorkersInflight();
	}

	/// <summary> Snapshot of number of work items in the queue (both being processed and waiting) </summary>
	public int CurrentQueued()
	{
		return _queue.Length();
	}

	/// <summary> Add an action to take when work is processed </summary>
	public Guid AddConsumer(Action<T> action)
	{
		lock (_lockObject)
		{
			var id = Guid.NewGuid();
			_workActions.Add(id, action);
			return id;
		}
	}

	/// <summary> Remove a previously added consumer </summary>
	public void RemoveConsumer(Guid consumerId)
	{
		lock (_lockObject)
		{
			_workActions.Remove(consumerId);
		}
	}

	/// <summary> Add a work item to process </summary>
	public void AddWork(T work)
	{
		_queue.Enqueue(work);
	}

	/// <summary> Add a named work item to process </summary>
	public void AddWork(T work, string? name)
	{
		_queue.Enqueue(work, name);
	}
		
	/// <summary> Add multiple work items to process </summary>
	public void AddWork(IEnumerable<T> workList)
	{
		foreach (var item in workList)
		{
			_queue.Enqueue(item);
		}
	}

	/// <summary> Add multiple work items to process, each with the same name </summary>
	public void AddWork(IEnumerable<T> workList, string? name)
	{
		foreach (var item in workList)
		{
			_queue.Enqueue(item, name);
		}
	}

	/// <summary> A snapshot of all consumers bound to this dispatcher at time of calling </summary>
	public IEnumerable<Action<T>> AllConsumers()
	{
		var snapshot = _workActions.Values.ToArray();
		return snapshot;
	}

	/// <summary> Event triggered when a consumer throws an exception </summary>
	public event EventHandler<ExceptionEventArgs<T>> Exceptions;

	/// <summary> Trigger to call when a consumer throws an exception </summary>
	public void OnExceptions(Exception e, IWorkQueueItem<T> work)
	{
		var handler = Exceptions;
		handler(this, new ExceptionEventArgs<T> { SourceException = e, WorkItem = work });
	}

	/// <summary> Start consuming work and continue until stopped </summary>
	public void Start()
	{
		lock (_lockObject)
		{
			if (!_workActions.Any())
				throw new InvalidOperationException("A dispatcher can't be started until it has at least one consumer");
			_pool.Start();
			Thread.Sleep(1);
		}
	}

	/// <summary> Stop consuming work and return when all in-progress work is complete </summary>
	public void Stop()
	{
		lock (_lockObject)
		{
			_pool.Stop();
		}
	}
		
	/// <summary>
	/// Continue consuming work and return when the queue reports 0 items waiting.
	/// If you continue to add work, this method will continue to block.
	/// </summary>
	public void WaitForEmptyQueueAndStop()
	{
		WaitForEmptyQueueAndStop(TimeSpan.MaxValue);
	}

	/// <summary>
	/// Continue consuming work and return when the queue reports 0 items waiting. 
	/// </summary>
	/// <param name="maxWait">Maximum duration to wait. The dispatcher will be stopped if this duration is exceeded</param>
	public void WaitForEmptyQueueAndStop(TimeSpan maxWait)
	{
		var sw = new Stopwatch();

		sw.Start();
		if (_queue is ICanStop stopper) stopper.StopAcceptingWork();
		
		while( (
			       _queue.BlockUntilReady() == QueueState.HasItems ||  // queue can read more items (including polling)
			       _queue.Length() > 0 ||                              // queue has items waiting locally
			       _pool.WorkersInflight() > 0                         // work is still on-going
		       )
			&& sw.Elapsed <= maxWait
		) { Thread.Sleep(100); }
		sw.Stop();

		Stop();
	}

	/// <summary>
	/// List the names of work items that are currently queued.
	/// Items that have been completed will not be listed.
	/// Items with no name provided will not be listed.
	/// </summary>
	public IEnumerable<string> ListNamedTasks()
	{
		return _queue.AllItemNames();
	}

	/// <summary>
	/// Set a custom sleeper for any waits that happen on this dispatch.
	/// There is a default sleeper that provides a limited linear back-off.
	/// </summary>
	public void SetSleeper(IBackOffWaiter sleeper)
	{
		_queue.SetSleeper(sleeper);
	}
}