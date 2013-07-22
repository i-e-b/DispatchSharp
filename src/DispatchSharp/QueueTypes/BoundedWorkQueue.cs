using System.Threading;

namespace DispatchSharp.QueueTypes
{
    public class BoundedWorkQueue<T> : IWorkQueue<T>
    {
        private readonly IWorkQueue<T> _queue;
        private readonly SemaphoreSlim _semaphore;

        public BoundedWorkQueue(IWorkQueue<T> queue, int bound)
        {
            _queue = queue;
            _semaphore = new SemaphoreSlim(0, bound);
            _semaphore.Release(bound);
        }

        public BoundedWorkQueue(int bound): this(new InMemoryWorkQueue<T>(), bound)
        {
        }

        public void Enqueue(T work)
        {
            _semaphore.Wait();
            try
            {
                _queue.Enqueue(work);
            }
            catch
            {
                _semaphore.Release();
                throw;
            }
        }

        public IWorkQueueItem<T> TryDequeue()
        {
            var workQueueItem = _queue.TryDequeue();
            if (workQueueItem.HasItem)
            {
                _semaphore.Release();
            }
            return workQueueItem;
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