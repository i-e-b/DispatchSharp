using System;
using System.Collections.Generic;
using DispatchSharp.Internal;

namespace DispatchSharp.QueueTypes
{
    /// <summary>
    /// A queue that implements waiting
    /// </summary>
    public class BoundedWorkQueue<T> : IWorkQueue<T>
    {
        private readonly IWorkQueue<Named<T>> _queue;
        private readonly IWaitHandle _waitHandle;

        /// <summary>
        /// Wrap an existing queue
        /// </summary>
        public BoundedWorkQueue(IWorkQueue<Named<T>> queue, int bound)
        {
            if (queue is BoundedWorkQueue<T>) throw new Exception("Bounded work queue can not wrap itself");
            _queue = queue;
            _waitHandle = new SemaphoreWait(bound, bound);
        }

        /// <summary>
        /// Start a new queue
        /// </summary>
        public BoundedWorkQueue(int bound): this(new InMemoryWorkQueue<Named<T>>(), bound)
        {
        }

        /// <inheritdoc />
        public void Enqueue(T work, string? name)
        {
            _waitHandle.WaitOne();
            try
            {
                _queue.Enqueue(new Named<T> { Value = work, Name = name });
            }
            catch
            {
                _waitHandle.Set();
                throw;
            }
        }

        /// <inheritdoc />
        public IWorkQueueItem<T> TryDequeue()
        {
            var workQueueItem = _queue.TryDequeue();
            if (workQueueItem.HasItem)
            {
                _waitHandle.Set();
            }
            return new BoundedWorkQueueItem<T>(workQueueItem, _waitHandle);
        }

        /// <inheritdoc />
        public int Length()
        {
            return _queue.Length();
        }

        /// <inheritdoc />
        public bool BlockUntilReady()
        {
            return _queue.BlockUntilReady();
        }

        /// <inheritdoc />
        public IEnumerable<string> AllItemNames() => _queue.AllItemNames();

        /// <summary>
        /// Ignored
        /// </summary>
        public void SetSleeper(IBackOffWaiter sleeper) { }
    }
}