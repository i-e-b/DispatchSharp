using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DispatchSharp.Internal;

namespace DispatchSharp.WorkerPools;

#pragma warning disable 420
/// <summary>
/// Worker pool that delegates work to happen on a set number of worker threads.
/// <p>This will use 'task parallel' mode, where consumers are run in parallel
/// and work items are handled in sequence</p>
/// <ul>
/// <li>Does not consume any more items than there are free workers.</li>
/// <li>Strictly obeys inflight limit from dispatcher.</li>
/// <li>Once a worker starts an item it will try to finish it even if the dispatcher is
/// shut down. Threads will be left to die after one minute.</li>
/// </ul>
/// </summary>
/// <typeparam name="T">Type of item on the work queue</typeparam>
public class TaskParallelThreadedWorkerPool<T> : IWorkerPool<T>
{
    private readonly object _incrementLock = new();
    private readonly object _threadPoolLock = new();
    private readonly object _workPullLock = new();

    private readonly string _name;
    private readonly List<Thread> _pool;
    private IDispatch<T> _dispatch;
    private IWorkQueue<T> _queue;
    private volatile object? _started;
    private volatile int _inflight;
    
    /// <summary> The current work item, or <c>null</c> if we need to pull more </summary>
    private volatile IWorkQueueItem<T>? _work;
    /// <summary> Consumers that are waiting to consume the current work item </summary>
    private readonly Queue<Action<T>> _waitingConsumers;

    /// <summary>
    /// Create a worker with a self-balancing pool of threads.
    /// </summary>
    /// <param name="name">Name of this worker pool (useful during debugging)</param>
    public TaskParallelThreadedWorkerPool(string? name) {
        _name = name ?? "UnnamedWorkerPool_" + typeof(T).Name;
        _pool = new List<Thread>();
        _waitingConsumers = new Queue<Action<T>>();
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
                TryJoinThread(thread, cantStopWarning);
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
    private static void TryJoinThread(Thread? thread, Action<Thread>? cantStopWarning)
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
                //thread.Abort(); // No longer possible
                cantStopWarning?.Invoke(thread);
            }
        }
        catch
        {
            TryThreadResetAbort();
        }
    }

    private static void TryThreadResetAbort()
    {
        try
        {
            Thread.ResetAbort(); // for .Net Framework only
        }
        catch (Exception)
        {
            // ignore
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
    private void WaitForQueueIfStillActive()
    {
        try
        {
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
            _queue.BlockUntilReady();
            Thread.CurrentThread.Priority = ThreadPriority.Normal;
        }
        catch (ThreadAbortException)
        {
            TryThreadResetAbort();
        }
    }


    /// <summary>
    /// This is the core loop that all worker threads run.
    /// </summary>
    private void WorkLoop(int index, object? reference)
    {
        bool ThereAreConsumersToRun() => _work is not null && _waitingConsumers.Count > 0;
        bool Running() => reference != null && _started == reference;
			
        while (Running())
        {
            if (ThreadIsNoLongerNeeded(index)) return;
            if (ThisIsControllerThread()) MaintainThreadPool();
            WaitForQueueIfStillActive();
            if (!Running()) return;
            
            // 1. If we have consumers not yet given work, dequeue and run;
            // 2. Otherwise, pull more work and refill the consumer queue.

            if (_work is null && ThisIsControllerThread())
            {
                lock (_workPullLock)
                {
                    //Console.Write($"t{index}? ");
                    _work = _queue.TryDequeue();
                    if (_work.HasItem)
                    {
                        foreach (var consumer in _dispatch.AllConsumers())
                        {
                            _waitingConsumers.Enqueue(consumer);
                        }
                    }
                    else
                    {
                        _work = null;
                    }
                }
            }

            lock (_incrementLock)
            {
                if (_inflight >= _dispatch.MaximumInflight()) continue;
                Interlocked.Increment(ref _inflight);
                //Console.Write($"t{index}> ");
            }
            
            while (true)
            {
                Action<T>? consumer;
                IWorkQueueItem<T> work;
                lock (_workPullLock)
                {
                    if (!Running()) break;
                    if (!ThereAreConsumersToRun()) break;
                    
                    if (_work is null) break;
                    work = _work;
                    
                    if (_waitingConsumers.Count < 1) break;
                    consumer = _waitingConsumers.Dequeue();
                }

                try
                {
                    //Console.Write($"t{index}w ");
                    consumer?.Invoke(_work.Item);
                }
                catch (Exception ex)
                {
                    TryFireExceptions(ex, work);
                }
            }

            Interlocked.Decrement(ref _inflight);

            // Complete the work once when inflight is zero and all consumers are gone
            if (ThisIsControllerThread())
            {
                while (_waitingConsumers.Count > 0)
                {
                    Thread.Sleep(100);
                }

                lock (_workPullLock)
                {
                    var endedWork = Interlocked.Exchange(ref _work, null);
                    endedWork?.Finish();
                }
            }
        }
    }

    private bool ThisIsControllerThread()
    {
        lock (_threadPoolLock)
        {
            if (_pool.Count < 1) return true;
            return Thread.CurrentThread.ManagedThreadId == _pool[0]?.ManagedThreadId;
        }
    }

    /// <summary> True if the inflight limit is reduced by the user </summary>
    private bool ThreadIsNoLongerNeeded(int index)
    {
        if (index < 1) return false; // always keep one thread open
        return index > _dispatch.MaximumInflight();
    }

    /// <summary> Send exception message callbacks if registered </summary>
    private void TryFireExceptions(Exception exception, IWorkQueueItem<T> work)
    {
        var dint = _dispatch as IDispatchInternal<T>;
        if (dint == null) return;

        dint.OnExceptions(exception, work);
    }

    /// <summary>
    /// Create a new worker thread and add it to the pool.
    /// The thread is returned not started -- you must call `Start()` yourself.
    /// </summary>
    private Thread? NewWorkerThread()
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

    private void SetStartedFlag()
    {
        var closedObject = new object();
        Interlocked.CompareExchange<object?>(ref _started, closedObject, null);
    }

    private void MaintainThreadPool()
    {
        if (_started == null) return;
        RemoveStoppedThreads();
        AddNewThreadsUntilConcurrencyLimit();
    }

    private void AddNewThreadsUntilConcurrencyLimit()
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

    private void RemoveStoppedThreads()
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