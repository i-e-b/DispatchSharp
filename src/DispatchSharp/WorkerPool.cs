using System;
using System.Linq;
using System.Threading;

namespace DispatchSharp.Unit.Tests
{
	public class WorkerPool<T> : IWorkerPool<T>
	{
		readonly string _name;
		readonly Thread[] _pool;
		IDispatch<T> _dispatch;
		volatile object _started;
		IWorkQueue<T> _queue;

		public WorkerPool(string name, int threadCount)
		{
			_name = name ?? "UnnamedWorkerPool";
			_pool = new Thread[threadCount];
			_started = null;
		}

		public void SetSource(IDispatch<T> dispatch, IWorkQueue<T> queue)
		{
			_dispatch = dispatch;
			_queue = queue;
		}

		public void Start()
		{
#pragma warning disable 420
			var closedObject = new object();
			if (Interlocked.CompareExchange(ref _started, closedObject, null) != null) return;
#pragma warning restore 420

			for (int i = 0; i < _pool.Length; i++)
			{
				_pool[i] = new Thread(() => WorkLoop(closedObject))
				{
					IsBackground = true,
					Name = _name + "_Thread_" + i
				};
				_pool[i].Start();
			}
		}

		void WorkLoop(object reference)
		{
			while (_started == reference)
			{
				_dispatch.Available.WaitOne();
				IWorkQueueItem<T> work;
				while ((work = _queue.TryDequeue()).HasItem)
				{
					foreach (var action in _dispatch.WorkActions().ToArray())
					{
						try
						{
							action(work.Item);
							work.Finish();
						}
						catch (Exception ex)
						{
							work.Cancel();
							_dispatch.OnExceptions(ex);
						}
					}
				}
			}
		}
	}
}