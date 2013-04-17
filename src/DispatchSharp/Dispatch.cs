using System;
using System.Collections.Generic;
using System.Threading;

namespace DispatchSharp.Unit.Tests
{
	public class Dispatch<T> : IDispatch<T>
	{
		readonly IWorkProvider<T> _provider;
		readonly IWorkerPool<T> _pool;
		readonly IList<Action<T>> _workActions;
		readonly object _lockObject;

		public WaitHandle Available { get; private set; }

		public Dispatch(IWorkProvider<T> workProvider, IWorkerPool<T> workerPool)
		{
			_provider = workProvider;
			_pool = workerPool;

			_lockObject = new object();
			_workActions = new List<Action<T>>();

			Available = new AutoResetEvent(true);
			_pool.SetSource(this, _provider);
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
			_provider.Enqueue(work);
			((AutoResetEvent)Available).Set();
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
			if (handler != null) handler(this, new ExceptionEventArgs{SourceException = e});
		}
	}
}