using System.Threading;
using DispatchSharp.Internal;

namespace DispatchSharp.QueueTypes
{
    /// <summary>
    /// A queue item that can be cancelled
    /// </summary>
    public class BoundedWorkQueueItem<T> : IWorkQueueItem<T>
    {
        private readonly IWorkQueueItem<T> _iwqi;
        private readonly IWaitHandle _waitHandle;
        private object _completionToken;

        /// <summary>
        /// Wrap a work item
        /// </summary>
        public BoundedWorkQueueItem(IWorkQueueItem<T> iwqi, IWaitHandle waitHandle)
        {
            _iwqi = iwqi;
            _waitHandle = waitHandle;
            _completionToken = new object();
        }

        /// <inheritdoc />
        public bool HasItem { get { return _iwqi.HasItem; } }

        /// <inheritdoc />
        public T Item { get { return _iwqi.Item; } }

        /// <inheritdoc />
        public void Finish()
        {
            Interlocked.Exchange(ref _completionToken, null);
            _iwqi.Finish();
        }

        /// <inheritdoc />
        public void Cancel()
        {
            var token = Interlocked.Exchange(ref _completionToken, null);

            if (token != null)
            {
                _waitHandle.WaitOne();
            }

            _iwqi.Cancel();
        }
    }
}