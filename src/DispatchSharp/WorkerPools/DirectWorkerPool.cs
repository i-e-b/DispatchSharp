using System;
using System.Threading;
using System.Linq;
using DispatchSharp.Internal;

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
		readonly IWaitHandle _started;

		public DirectWorkerPool()
		{
			_started = new CrossThreadWait(false);
		}

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
			_started.Reset();
			_worker = new Thread(DoWork){IsBackground = true, Name = "SingleThreadedWorker_"+typeof(T).Name};
			_worker.Start();
			_started.WaitOne();
		}

		public void Stop()
		{
			_running = false;

			var local = Interlocked.Exchange(ref _worker, null);
			if (local == null) return;

			while (!local.Join(1000))
			{
				Thread.Sleep(500);
			}
		}

		void DoWork()
		{
			Func<bool> started = () => { _started.Set(); return false; };
			while (_running) {
				IWorkQueueItem<T> work;
				while ((work = _queue.TryDequeue()).HasItem || started())
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