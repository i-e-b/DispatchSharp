using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		readonly string _name;
		readonly List<Thread> _pool;
		IDispatch<T> _dispatch;
		IWorkQueue<T> _queue;
		volatile object? _started;
		volatile int _inflight;

		/// <summary>
		/// Create a worker with a self-balancing pool of threads.
		/// </summary>
		/// <param name="name">Name of this worker pool (useful during debugging)</param>
		public ThreadedWorkerPool(string? name) {
			_name = name ?? "UnnamedWorkerPool_" + typeof(T).Name;
			_pool = new List<Thread>();
			_dispatch= UninitialisedValues.InvalidDispatch<T>();
			_queue = UninitialisedValues.InvalidQueue<T>();
			_started = null;
			_inflight = 0;
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
			SetStartedFlag();
			MaintainThreadPool(); // this one will boot-strap all the other workers up to the dispatcher limit
		}

		/// <summary>
		/// Stop processing incoming queue items.
		/// Current work must be finished or cancelled before returning.
		/// </summary>
		public void Stop(Action<Thread>? cantStopWarning = null)
		{
			_started = null;
			while (_inflight > 0) Thread.Sleep(10);

			lock (_threadPoolLock)
			{
				if (_pool == null) throw new Exception("stop error!");
				var toKill = _pool.ToArray();

				var sw = new Stopwatch();
				sw.Start();
				foreach (var thread in toKill)
				{
					TryJoinThread(thread);
				}

				foreach (var thread in toKill)
				{
					cantStopWarning?.Invoke(thread);
				}
			}
		}

		/// <summary>
		/// Wait for working threads to finish (up to 1 minute)
		/// Kill waiting threads immediately.
		/// 
		/// Waiting threads are marked by a non-normal thread priority
		/// </summary>
		static void TryJoinThread(Thread? thread)
		{
			if (thread == null) return;
			if (!thread.IsAlive) return;
			try
			{
				if (thread.Priority == ThreadPriority.Normal)
				{
					thread.Join(10); // return from here if working
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
            lock (_threadPoolLock)
            {
                if (_pool == null) throw new Exception("Pool was null. Maybe this object has been disposed?");
                return _pool.Count;
            }
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


		/// <summary>
		/// This is the core loop that all worker threads run.
		/// </summary>
		void WorkLoop(int index, object? reference)
		{
			bool Running() => reference != null && _started == reference;
			
			while (Running())
			{
				if (ThreadIsNoLongerNeeded(index)) return;
				if (index == 0) MaintainThreadPool();
				WaitForQueueIfStillActive();
				if (!Running()) return;

				lock (_incrementLock)
				{
					if (_inflight >= _dispatch.MaximumInflight()) continue;
					Interlocked.Increment(ref _inflight);
				}

				IWorkQueueItem<T> work;
				while (Running() && (work = _queue.TryDequeue()).HasItem)
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
		/// The thread is returned not started -- you must call `Start()` yourself.
		/// </summary>
		Thread? NewWorkerThread()
		{
			lock (_threadPoolLock)
			{
				var threadIndex = _pool.Count;
				if (_started == null) return null;
				var newThread = new Thread(() => WorkLoop(threadIndex, _started))
				{
					IsBackground = true,
					Name = _name + "_Thread_" + threadIndex,
					Priority = ThreadPriority.BelowNormal
				};
				_pool.Add(newThread);
				return newThread;
			}
		}

		void SetStartedFlag()
		{
			var closedObject = new object();
			Interlocked.CompareExchange<object?>(ref _started, closedObject, null);
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
					var t = NewWorkerThread();
					if (t != null) t.Start();
				}
			}
		}

		void RemoveStoppedThreads()
		{
			lock (_threadPoolLock)
			{
				if (_pool.All(t => t != null && t.IsAlive)) return;
				var alive = _pool.Where(t => t.IsAlive).ToList();
				_pool.Clear();
				_pool.AddRange(alive);
			}
		}
	}
}