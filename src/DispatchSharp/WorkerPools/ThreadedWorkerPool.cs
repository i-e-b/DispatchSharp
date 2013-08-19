using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DispatchSharp.Internal;

// ignore ref/volatile conflict:
#pragma warning disable 420

namespace DispatchSharp.WorkerPools
{
	/// <summary>
	/// Worker pool that delegates work to happen on a set number of worker threads.
	/// Does not consume any more items than there are free workers.
	/// Strictly obeys inflight limit from dispatcher.
	/// Once a worker starts an item it will try to finish it even if the dispatcher is
	/// shut down. Threads will be left to die after one minute.
	/// </summary>
	/// <typeparam name="T">Type of item on the work queue</typeparam>
	public class ThreadedWorkerPool<T> : IWorkerPool<T>
	{
		readonly int _threadCount;
		const int OneMinute = 60000;
		readonly string _name;
		readonly List<Thread> _pool;
		IDispatch<T> _dispatch;
		IWorkQueue<T> _queue;
		volatile object _started;
		volatile int _inflight;
		private readonly object _incrementLock;

		/// <summary>
		/// Create a worker pool with a specific number of threads. 
		/// </summary>
		/// <param name="name">Name of this worker pool (useful during debugging)</param>
		/// <param name="threadCount">Number of threads to pool</param>
		public ThreadedWorkerPool(string name, int threadCount)
		{
			_threadCount = threadCount;
			if (threadCount < 1) throw new ArgumentException("thread count must be at least one", "threadCount");
			if (threadCount > 1000) throw new ArgumentException("thread count should not be more than 1000", "threadCount");

			_name = name ?? "UnnamedWorkerPool_" + typeof(T).Name;
			_pool = new List<Thread>();
			_started = null;
			_inflight = 0;
			_incrementLock = new object();
		}

		/// <summary>
		/// Create a worker pool with a thread per logical cpu core. 
		/// </summary>
		/// <param name="name">Name of this worker pool (useful during debugging)</param>
		public ThreadedWorkerPool(string name) : this(name, Default.ThreadCount) { }

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
			var closedObject = new object();
			if (Interlocked.CompareExchange(ref _started, closedObject, null) != null) return;

			for (int i = 0; i < _threadCount; i++)
			{
				var threadIndex = i;
				var newThread = new Thread(() => WorkLoop(threadIndex, closedObject))
				{
					IsBackground = true,
					Name = _name + "_Thread_" + threadIndex
				};
				newThread.Start();
				_pool.Add(newThread);
			}
		}

		/// <summary>
		/// Stop processing incoming queue items.
		/// Current work should be finished or cancelled before returning.
		/// </summary>
		public void Stop()
		{
			_started = null;
			while (_inflight > 0) Thread.Sleep(10);

			if (_pool == null) throw new Exception("stop error!");
			foreach (var thread in _pool) SafeKillThread(thread);
		}

		/// <summary>
		/// Wait for working threads to finish (up to 1 minute)
		/// Kill waiting threads immediately.
		/// 
		/// Waiting threads are marked by a non-normal thread priority
		/// </summary>
		static void SafeKillThread(Thread thread)
		{
			if (thread == null) return;
			if (!thread.IsAlive) return;
			try
			{
				if (thread.Priority == ThreadPriority.Normal)
				{
					thread.Join(OneMinute);
				}
				else
				{
					thread.Abort();
				}
			}
			catch
			{
				Thread.ResetAbort();
			}
		}

		/// <summary>
		/// Current number of workers running actions against queue items
		/// </summary>
		public int WorkersInflight()
		{
			return _inflight;
		}

		/// <summary>
		/// Returns number of worker threads in use
		/// </summary>
		public int PoolSize()
		{
			if (_pool == null) return 0;
			return _threadCount;
		}

		void WorkLoop(int index, object reference)
		{
			Func<bool> running = () => _started == reference;
			while (running())
			{
				WaitForQueueIfStillActive(index);
				if (!running()) return;

				lock (_incrementLock)
				{
					if (_inflight >= _dispatch.MaximumInflight()) continue;
					Interlocked.Increment(ref _inflight);
				}

				IWorkQueueItem<T> work;
				while (running() && (work = _queue.TryDequeue()).HasItem)
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
				Interlocked.Decrement(ref _inflight);
			}
		}

		void BlockUntilUsable(int threadIndex)
		{
			while (threadIndex >= _dispatch.MaximumInflight())
			{
				Thread.Sleep(500);
			}
		}

		void TryFireExceptions(Exception exception, IWorkQueueItem<T> work)
		{
			var dint = _dispatch as IDispatchInternal<T>;
			if (dint == null) return;

			dint.OnExceptions(exception, work);
		}

		/// <summary>
		/// Mark the thread as low priority while it is waiting for queue work.
		/// </summary>
		void WaitForQueueIfStillActive(int threadIndex)
		{
			try
			{
				Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
				BlockUntilUsable(threadIndex);
				_queue.BlockUntilReady();
				Thread.CurrentThread.Priority = ThreadPriority.Normal;
			}
			catch (ThreadAbortException)
			{
				Thread.ResetAbort();
			}
		}
	}
}