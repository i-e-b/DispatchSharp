using System;
using System.Threading;
using System.Linq;

namespace DispatchSharp.WorkerPools
{
	/// <summary>
	/// Single threaded on-demand pool for integration testing
	/// </summary>
	public class DirectWorkerPool<T> : IWorkerPool<T>
	{
		IDispatch<T> _dispatch;
		IWorkQueue<T> _queue;
		Thread _worker;
		volatile bool _running = true;

		public void SetSource(IDispatch<T> dispatch, IWorkQueue<T> queue)
		{
			_dispatch = dispatch;
			_queue = queue;
		}

		public void Start()
		{
			var safety = Interlocked.CompareExchange(ref _worker, null, null);
			if (safety != null) return;

			_running = true;
			_worker = new Thread(DoWork){IsBackground = true};
			_worker.Start();
		}

		public void Stop()
		{
			var local = Interlocked.Exchange(ref _worker, null);
			if (local == null) return;

			_running = false;
			local.Join();
		}

		void DoWork()
		{
			IWorkQueueItem<T> work;
			while (_running) {
				_queue.BlockUntilReady();
				while ((work = _queue.TryDequeue()).HasItem)
				{
					foreach (var action in _dispatch.AllConsumers().ToArray())
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

		public int WorkersInflight()
		{
			return 0;
		}
	}
}