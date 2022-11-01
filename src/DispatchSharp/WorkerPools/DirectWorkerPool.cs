using System;
using System.Threading;
using System.Linq;
using DispatchSharp.Internal;

namespace DispatchSharp.WorkerPools
{
	/// <summary>
	/// Single threaded on-demand pool for integration testing
	/// WARNING: this pool with continue to work if the queue is kept populated
	/// </summary>
	public class DirectWorkerPool<T> : IWorkerPool<T>
	{
		IDispatch<T> _dispatch;
		IWorkQueue<T> _queue;
		Thread? _worker;
		volatile bool _running = true;
		readonly IWaitHandle _started;

		/// <summary>
		/// Create a new direct worker pool
		/// </summary>
		public DirectWorkerPool()
		{
			_dispatch = UninitialisedValues.InvalidDispatch<T>();
			_queue = UninitialisedValues.InvalidQueue<T>();
			_started = new CrossThreadWait(false);
		}

		/// <summary>
		/// Set source queue and managing dispatcher
		/// </summary>
		public void SetSource(IDispatch<T> dispatch, IWorkQueue<T> queue)
		{
			_dispatch = dispatch;
			_queue = queue;
		}

		/// <summary>
		/// Start processing queue items as they become available
		/// </summary>
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

		/// <summary>
		/// Stop processing once work queue is exhausted.
		/// WARNING: this pool with continue to work if the queue is kept populated
		/// </summary>
		public void Stop(Action<Thread>? cantStopWarning = null)
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
			bool Started()
			{
				_started.Set();
				return false;
			}

			while (_running) {
				IWorkQueueItem<T> work;
				while ((work = _queue.TryDequeue()).HasItem || Started())
				{
					foreach (var action in _dispatch.AllConsumers().ToArray())
					{
						try
						{
							action(work.Item);
						}
						catch (Exception ex)
						{
							TryFireExceptions(ex, work);
						}
						finally
						{
							work.Finish();
						}
					}
				}
			}
		}

		void TryFireExceptions(Exception exception, IWorkQueueItem<T> work)
		{
			var dint = _dispatch as IDispatchInternal<T>;
			if (dint == null) return;

			dint.OnExceptions(exception, work);
		}

		/// <summary>
		/// Current number of workers running actions against queue items
		/// </summary>
		public int WorkersInflight()
		{
			return 0;
		}

		/// <summary>
		/// Returns number of worker threads in use
		/// </summary>
		public int PoolSize()
		{
			return 1;
		}
	}
}