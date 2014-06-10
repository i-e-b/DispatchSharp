using System.Threading;
using DispatchSharp.QueueTypes;
using DispatchSharp.WorkerPools;
using NSubstitute;
using NUnit.Framework;

namespace DispatchSharp.Unit.Tests
{
    [TestFixture, Category(Categories.FastTests)]
	public class direct_worker_pool_tests
	{
		
		IDispatch<object> _dispatcher;
		IWorkerPool<object> _subject;
		IWorkQueue<object> _queue;
		 
		
		[SetUp]
		public void setup ()
		{
			_dispatcher = Substitute.For<IDispatch<object>>();
			_queue = new InMemoryWorkQueue<object>();
			_subject = new DirectWorkerPool<object>();
			_subject.SetSource(_dispatcher, _queue);
		}
		
		[Test]
		public void waits_for_started_message ()
		{
			var mockQueue = Substitute.For<IWorkQueue<object>>();
			_subject.SetSource(_dispatcher, mockQueue);

			mockQueue.DidNotReceive().TryDequeue();
			_subject.Start();
			mockQueue.Received().TryDequeue();
		}

		[Test]
		public void completes_all_tasks_before_stopping ()
		{
			_queue.Enqueue(new object());
			_queue.Enqueue(new object());

			_dispatcher.DidNotReceive().AllConsumers();

			_subject.Start();
			_subject.Stop();
			
			_dispatcher.Received(2).AllConsumers();
		}

		[Test]
		public void processes_all_available_work_as_soon_as_available ()
		{
			_subject.Start();

			_dispatcher.DidNotReceive().AllConsumers();
			_queue.Enqueue(new object());
			_queue.Enqueue(new object());
			Thread.Sleep(100);
			_dispatcher.Received(2).AllConsumers();

			_subject.Stop();
		}
	}
}