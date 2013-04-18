using System;
using System.Collections.Generic;
using System.Threading;

namespace DispatchSharp
{

	public class Dispatch<T> : IDispatch<T>
	{
		readonly IWorkQueue<T> _queue;
		readonly IWorkerPool<T> _pool;
		readonly IList<Action<T>> _workActions;
		readonly object _lockObject;

		public IWaitHandle Available { get; private set; }

		public Dispatch(IWorkQueue<T> workQueue, IWorkerPool<T> workerPool)
		{
			_queue = workQueue;
			_pool = workerPool;

			_lockObject = new object();
			_workActions = new List<Action<T>>();

			Available = new AutoResetEventWrapper(true);
			_pool.SetSource(this, _queue);
		}

		public void AddConsumer(Action<T> action)
		{
			_pool.Start();
			lock (_lockObject)
			{
				_workActions.Add(action);
			}
		}

		public void AddWork(T work)
		{
			_queue.Enqueue(work);
			Available.Set();
		}

		public IEnumerable<Action<T>> WorkActions()
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

		public void Stop()
		{
			_pool.Stop();
		}

		class AutoResetEventWrapper : IWaitHandle
		{
			readonly AutoResetEvent _base;

			public AutoResetEventWrapper(bool initialSetting)
			{
				_base = new AutoResetEvent(initialSetting);
			}
			public bool WaitOne()
			{
				return _base.WaitOne();
			}

			public void Set()
			{
				_base.Set();
			}

			public void Reset()
			{
				_base.Reset();
			}
		}
	}
}