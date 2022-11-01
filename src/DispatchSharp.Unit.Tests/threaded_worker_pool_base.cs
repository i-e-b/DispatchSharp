using System.Threading;
using NSubstitute;
// ReSharper disable InconsistentNaming
// ReSharper disable PossibleNullReferenceException

namespace DispatchSharp.Unit.Tests
{
	public class threaded_worker_pool_base
	{
		protected IDispatch<object>? _dispatcher;
		protected IWorkerPool<object>? _subject;
		protected IWorkQueue<object>? _queue;

		protected void Go()
		{
			_subject.Start();
			Thread.Sleep(20);
			_subject.Stop();
		}

		protected void ItemAvailable(bool yes)
		{
			var item = Substitute.For<IWorkQueueItem<object>>();
			item.HasItem.Returns(yes);
			_queue.TryDequeue().Returns(item);
		}
	}
}