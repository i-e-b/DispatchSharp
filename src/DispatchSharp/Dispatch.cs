using System;
using System.Collections.Generic;
using System.Linq;
using DispatchSharp.Internal;

namespace DispatchSharp
{
	public class Dispatch<T> : IDispatch<T>
	{
		readonly IWorkQueue<T> _queue;
		readonly IWorkerPool<T> _pool;
		readonly IList<Action<T>> _workActions;
		readonly object _lockObject;

		public Dispatch(IWorkQueue<T> workQueue, IWorkerPool<T> workerPool)
		{
			MaximumInflight = Default.ThreadCount;

			_queue = workQueue;
			_pool = workerPool;

			_lockObject = new object();
			_workActions = new List<Action<T>>();

			_pool.SetSource(this, _queue);
		}

		public int MaximumInflight { get; set; }

		public int CurrentInflight()
		{
			return _pool.WorkersInflight();
		}

		public void AddConsumer(Action<T> action)
		{
			lock (_lockObject)
			{
				_workActions.Add(action);
			}
		}

		public void AddWork(T work)
		{
			_queue.Enqueue(work);
		}

		public IEnumerable<Action<T>> AllConsumers()
		{
			lock (_lockObject)
			{
				foreach (var workAction in _workActions)
				{
					yield return workAction;
				}
			}
		}

		public event EventHandler<ExceptionEventArgs> Exceptions;

		public void OnExceptions(Exception e)
		{
			var handler = Exceptions;
			if (handler != null) handler(this, new ExceptionEventArgs { SourceException = e });
		}

		public void Start()
		{
			lock (_lockObject)
			{
				if (!_workActions.Any())
					throw new InvalidOperationException("A dispatcher can't be started until it has at least one consumer");
				_pool.Start();
			}
		}

		public void Stop()
		{
			lock (_lockObject)
			{
				_pool.Stop();
			}
		}
	}
}