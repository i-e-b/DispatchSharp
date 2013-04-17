using System;
using System.Linq;
using System.Threading;

namespace DispatchSharp.Unit.Tests
{
	public class WorkerPool<T> : IWorkerPool<T>
	{
		readonly Thread[] _pool;
		IDispatch<T> _dispatch;
		volatile object _started;
		IWorkProvider<T> _provider;

		public WorkerPool(int threadCount)
		{
			_pool = new Thread[threadCount];
			_started = null;
		}

		public void SetSource(IDispatch<T> dispatch, IWorkProvider<T> provider)
		{
			_dispatch = dispatch;
			_provider = provider;
		}

		public void Start()
		{
			// ReSharper disable CSharpWarnings::CS0420
			var closedObject = new object();
			if (Interlocked.CompareExchange(ref _started, closedObject, null) != null) return;
			// ReSharper restore CSharpWarnings::CS0420

			for (int i = 0; i < _pool.Length; i++)
			{
				_pool[i] = new Thread(() => WorkLoop(closedObject))
				{
					IsBackground = true,
					Name = "Pool_" + i
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
				while ((work = _provider.TryDequeue()).HasItem)
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