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
		readonly object _incrementLock = new object();
		readonly object _threadPoolLock = new object();

		const int OneMinute = 60000;
		readonly string _name;
		readonly List<Thread> _pool;
		IDispatch<T> _dispatch;
		IWorkQueue<T> _queue;
		volatile object _started;
		volatile int _inflight;

		/// <summary>
		/// Create a worker pool with a specific number of threads. 
		/// </summary>
		/// <param name="name">Name of this worker pool (useful during debugging)</param>
		/// <param name="threadCount">Number of threads to pool</param>
		public ThreadedWorkerPool(string name, int threadCount)
		{
			if (threadCount < 1) throw new ArgumentException("thread count must be at least one", "threadCount");
			if (threadCount > 1000) throw new ArgumentException("thread count should not be more than 1000", "threadCount");

			_name = name ?? "UnnamedWorkerPool_" + typeof(T).Name;
			_pool = new List<Thread>();
			_started = null;
			_inflight = 0;
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
			if (SetStartedFlag()) return;
			NewWorkerThread().Start(); // this one will boot-strap all the other workers up to the dispatcher limit
		}

		/// <summary>
		/// Stop processing incoming queue items.
		/// Current work should be finished or cancelled before returning.
		/// </summary>
		public void Stop()
		{
			_started = null;
			while (_inflight > 0) Thread.Sleep(10);

			lock (_threadPoolLock)
			{
				if (_pool == null) throw new Exception("stop error!");
				var toKill = _pool.ToArray();
				foreach (var thread in toKill) SafeKillThread(thread);
			}
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
			if (_pool == null) throw new Exception("Pool was null. Maybe this object has been disposed?");
			return _pool.Count;
		}

		/// <summary>
		/// Mark the thread as low priority while it is waiting for queue work.
		/// </summary>
		void WaitForQueueIfStillActive()
		{
			try
			{
				Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
				_queue.BlockUntilReady();
				Thread.CurrentThread.Priority = ThreadPriority.Normal;
			}
			catch (ThreadAbortException)
			{
				Thread.ResetAbort();
			}
		}

		void WorkLoop(int index, object reference)
		{
			Func<bool> running = () => reference != null && _started == reference;
			while (running())
			{
				if (ThreadIsNoLongerNeeded(index)) return;
				if (index == 0) MaintainThreadPool();
				WaitForQueueIfStillActive();
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

		bool ThreadIsNoLongerNeeded(int index)
		{
			if (index < 1) return false; // always keep one thread open
			return index > _dispatch.MaximumInflight();
		}

		void TryFireExceptions(Exception exception, IWorkQueueItem<T> work)
		{
			var dint = _dispatch as IDispatchInternal<T>;
			if (dint == null) return;

			dint.OnExceptions(exception, work);
		}

		/// <summary>
		/// Create a new worker thread and add it to the pool.
		/// The thread is returned unstarted.
		/// </summary>
		Thread NewWorkerThread()
		{
			lock (_threadPoolLock)
			{
				var threadIndex = _pool.Count;
				if (_started == null) return null;
				var newThread = new Thread(() => WorkLoop(threadIndex, _started))
				{
					IsBackground = true,
					Name = _name + "_Thread_" + threadIndex
				};
				_pool.Add(newThread);
				return newThread;
			}
		}

		bool SetStartedFlag()
		{
			var closedObject = new object();
			return Interlocked.CompareExchange(ref _started, closedObject, null) != null;
		}

		void MaintainThreadPool()
		{
			if (_started == null) return;
			RemoveStoppedThreads();
			AddNewThreadsUntilConcurrencyLimit();
		}

		void AddNewThreadsUntilConcurrencyLimit()
		{
			lock (_threadPoolLock)
			{
				int safeInflightLimit = Math.Min(100, _dispatch.MaximumInflight());
				int missing = safeInflightLimit - _pool.Count;
				if (missing < 1) return;

				for (int i = 0; i < missing; i++)
				{
					NewWorkerThread().Start();
				}
			}
		}

		void RemoveStoppedThreads()
		{
			lock (_threadPoolLock)
			{
				if (_pool.All(t=>t.IsAlive)) return;
				var alive = _pool.Where(t => t.IsAlive).ToList();
				_pool.Clear();
				_pool.AddRange(alive);
			}
		}
	}
}