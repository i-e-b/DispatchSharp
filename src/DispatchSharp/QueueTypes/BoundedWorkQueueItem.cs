using System.Threading;
using DispatchSharp.Internal;

namespace DispatchSharp.QueueTypes
{
    /// <summary>
    /// A queue item that can be cancelled
    /// </summary>
    public class BoundedWorkQueueItem<T> : IWorkQueueItem<T>
    {
        private readonly IWorkQueueItem<T> _workQueueItem;
        private readonly IWaitHandle _waitHandle;
        private object? _completionToken;

        /// <summary>
        /// Wrap a work item
        /// </summary>
        public BoundedWorkQueueItem(IWorkQueueItem<T> workQueueItem, IWaitHandle waitHandle)
        {
            _workQueueItem = workQueueItem;
            _waitHandle = waitHandle;
            _completionToken = new object();
        }

        /// <inheritdoc />
        public bool HasItem => _workQueueItem.HasItem;

        /// <inheritdoc />
        public T Item => _workQueueItem.Item;

        /// <inheritdoc />
        public void Finish()
        {
            Interlocked.Exchange<object?>(ref _completionToken, null);
            _workQueueItem.Finish();
        }

        /// <inheritdoc />
        public void Cancel()
        {
            var token = Interlocked.Exchange<object?>(ref _completionToken, null);

            if (token != null)
            {
                _waitHandle.WaitOne();
            }

            _workQueueItem.Cancel();
        }
    }
}