using System.Threading;
using DispatchSharp.WorkerPools;
using NSubstitute;
using NUnit.Framework;

namespace DispatchSharp.Unit.Tests
{
	[TestFixture]
	public class direct_worker_pool_tests
	{
		
		IDispatch<object> _dispatcher;
		IWorkerPool<object> _subject;
		IWorkQueue<object> _queue;
		 
		
		[SetUp]
		public void setup ()
		{
			_dispatcher = Substitute.For<IDispatch<object>>();
			_queue = Substitute.For<IWorkQueue<object>>();
			_subject = new DirectWorkerPool<object>();
			_subject.SetSource(_dispatcher, _queue);
		}
		
		[Test]
		public void ignores_started_message ()
		{
			//_subject.Start();
			_queue.DidNotReceive().TryDequeue();
		}

		[Test]
		public void ignores_stopped_message ()
		{
			_subject.Start();
			_subject.Stop();

			//_subject.TriggerAvailable();
			
			_queue.Received().TryDequeue();
		}

		[Test]
		public void processes_all_available_work_as_soon_as_the_availability_trigger_is_set ()
		{
			//_subject.TriggerAvailable();
			//_subject.TriggerAvailable();
			_queue.Received(2).TryDequeue();
		}
	}
}