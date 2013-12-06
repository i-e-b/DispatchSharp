using System;
using System.Threading;
using NSubstitute;

namespace DispatchSharp.Unit.Tests
{
	public class threaded_worker_pool_base
	{
		public IDispatch<object> _dispatcher;
		public IWorkerPool<object> _subject;
		public IWorkQueue<object> _queue;
		
		public void Go()
		{
			_subject.Start();
			Thread.Sleep(20);
			_subject.Stop(TimeSpan.FromSeconds(10));
		}
		public void ItemAvailable(bool yes)
		{
			var item = Substitute.For<IWorkQueueItem<object>>();
			item.HasItem.Returns(yes);
			_queue.TryDequeue().Returns(item);
		}
	}
}