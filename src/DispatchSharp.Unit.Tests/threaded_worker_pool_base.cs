using System.Threading;
using DispatchSharp.WorkerPools;
using NSubstitute;

namespace DispatchSharp.Unit.Tests
{
	public class threaded_worker_pool_base
	{
		public IDispatch<object> _dispatcher;
		public IWorkerPool<object> _subject;
		public IWorkQueue<object> _queue;
		

		public void Available(bool b)
		{
			if (b) ((ThreadedWorkerPool<object>)_subject).Available.Set();
			else ((ThreadedWorkerPool<object>)_subject).Available.Reset();
		}
		public void Go()
		{
			_subject.Start();
			Thread.Sleep(20);
			_subject.Stop();
		}
		public void ItemAvailable(bool yes)
		{
			var item = Substitute.For<IWorkQueueItem<object>>();
			item.HasItem.Returns(yes);
			_queue.TryDequeue().Returns(item);
		}
	}
}