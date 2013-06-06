using System;
using System.Collections.Generic;
using System.Linq;
using DispatchSharp.Internal;
using DispatchSharp.WorkerPools;

namespace DispatchSharp
{
	/// <summary>
	/// Default dispatcher
	/// </summary>
	/// <typeparam name="T">Type of work item to be processed</typeparam>
	public partial class Dispatch<T> : IDispatch<T>
	{
		readonly IWorkQueue<T> _queue;
		readonly IWorkerPool<T> _pool;
		readonly IList<Action<T>> _workActions;
		readonly object _lockObject;
		int _maximumInflight;

		/// <summary>
		/// Create a dispatcher with a specific queue and worker pool
		/// </summary>
		public Dispatch(IWorkQueue<T> workQueue, IWorkerPool<T> workerPool)
		{
			_maximumInflight = workerPool.PoolSize();

			_queue = workQueue;
			_pool = workerPool;

			_lockObject = new object();
			_workActions = new List<Action<T>>();

			_pool.SetSource(this, _queue);
		}

		/// <summary> Maximum number of work items being processed at any one time </summary>
		public int MaximumInflight()
		{
			return _maximumInflight; }

		/// <summary>
		/// Maximum number of work items being processed at any one time
		/// </summary>
		public void SetMaximumInflight(int max) {
			 _maximumInflight = max;
		}

		/// <summary> Snapshot of number of work items being processed </summary>
		public int CurrentInflight()
		{
			return _pool.WorkersInflight();
		}

		/// <summary> Add an action to take when work is processed </summary>
		public void AddConsumer(Action<T> action)
		{
			lock (_lockObject)
			{
				_workActions.Add(action);
			}
		}

		/// <summary> Add a work item to process </summary>
		public void AddWork(T work)
		{
			_queue.Enqueue(work);
		}

		/// <summary> Add multiple work items to process </summary>
		public void AddWork(IEnumerable<T> workList)
		{
			foreach (var item in workList)
			{
				_queue.Enqueue(item);
			}
		}

		/// <summary> All consumers added to this dispatcher </summary>
		public IEnumerable<Action<T>> AllConsumers()
		{
			foreach (var workAction in _workActions)
			{
				yield return workAction;
			}
		}

		/// <summary> Event triggered when a consumer throws an exception </summary>
		public event EventHandler<ExceptionEventArgs> Exceptions;

		/// <summary> Trigger to call when a consumer throws an exception </summary>
		public void OnExceptions(Exception e)
		{
			var handler = Exceptions;
			if (handler != null) handler(this, new ExceptionEventArgs { SourceException = e });
		}

		/// <summary> Start consuming work and continue until stopped </summary>
		public void Start()
		{
			lock (_lockObject)
			{
				if (!_workActions.Any())
					throw new InvalidOperationException("A dispatcher can't be started until it has at least one consumer");
				_pool.Start();
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
			while(_queue.BlockUntilReady() || _queue.Length() > 0) { }
			Stop();
		}
	}
}