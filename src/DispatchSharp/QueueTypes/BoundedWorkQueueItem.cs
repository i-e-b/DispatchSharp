using System.Threading;
using DispatchSharp.Internal;

namespace DispatchSharp.QueueTypes
{
    public class BoundedWorkQueueItem<T> : IWorkQueueItem<T>
    {
        private readonly IWorkQueueItem<T> _iwqi;
        private readonly IWaitHandle _waitHandle;
        private object _completionToken;

        public BoundedWorkQueueItem(IWorkQueueItem<T> iwqi, IWaitHandle waitHandle)
        {
            _iwqi = iwqi;
            _waitHandle = waitHandle;
            _completionToken = new object();
        }

        public bool HasItem { get { return _iwqi.HasItem; } }
        public T Item { get { return _iwqi.Item; } }
        public void Finish()
        {
            Interlocked.Exchange(ref _completionToken, null);
            _iwqi.Finish();
        }

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