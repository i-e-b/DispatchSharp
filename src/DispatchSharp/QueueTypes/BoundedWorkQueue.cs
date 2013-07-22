using DispatchSharp.Internal;

namespace DispatchSharp.QueueTypes
{
    public class BoundedWorkQueue<T> : IWorkQueue<T>
    {
        private readonly IWorkQueue<T> _queue;
        private readonly IWaitHandle _waitHandle;

        public BoundedWorkQueue(IWorkQueue<T> queue, int bound)
        {
            _queue = queue;
            _waitHandle = new SemaphoreWait(bound, bound);
        }

        public BoundedWorkQueue(int bound): this(new InMemoryWorkQueue<T>(), bound)
        {
        }

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

        public IWorkQueueItem<T> TryDequeue()
        {
            var workQueueItem = _queue.TryDequeue();
            if (workQueueItem.HasItem)
            {
                _waitHandle.Set();
            }
            return new BoundedWorkQueueItem<T>(workQueueItem, _waitHandle);
        }


        public int Length()
        {
            return _queue.Length();
        }

        public bool BlockUntilReady()
        {
            return _queue.BlockUntilReady();
        }
    }
}