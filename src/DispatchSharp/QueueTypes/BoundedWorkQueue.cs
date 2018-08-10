using DispatchSharp.Internal;

namespace DispatchSharp.QueueTypes
{
    /// <summary>
    /// A queue that implements waiting
    /// </summary>
    public class BoundedWorkQueue<T> : IWorkQueue<T>
    {
        private readonly IWorkQueue<T> _queue;
        private readonly IWaitHandle _waitHandle;

        /// <summary>
        /// Wrap an existing queue
        /// </summary>
        public BoundedWorkQueue(IWorkQueue<T> queue, int bound)
        {
            _queue = queue;
            _waitHandle = new SemaphoreWait(bound, bound);
        }

        /// <summary>
        /// Start a new queue
        /// </summary>
        public BoundedWorkQueue(int bound): this(new InMemoryWorkQueue<T>(), bound)
        {
        }

        /// <inheritdoc />
        public void Enqueue(T work)
        {
            _waitHandle.WaitOne();
            try
            {
                _queue.Enqueue(work);
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
    }
}