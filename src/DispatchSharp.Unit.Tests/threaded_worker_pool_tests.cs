using System.Threading;
using DispatchSharp.WorkerPools;
using NSubstitute;
using NUnit.Framework;

namespace DispatchSharp.Unit.Tests
{
	[TestFixture]
	public class threaded_worker_pool_tests:threaded_worker_pool_base
	{
		[SetUp]
		public void setup ()
		{
			_dispatcher = Substitute.For<IDispatch<object>>();
			_dispatcher.MaximumInflight.Returns(4);
			_queue = Substitute.For<IWorkQueue<object>>();
			_subject = new ThreadedWorkerPool<object>("name", 4);
			_subject.SetSource(_dispatcher, _queue);
		}

		[Test]
		public void worker_pool_does_nothing_if_not_started ()
		{
			Thread.Sleep(250);
			_queue.DidNotReceive().TryDequeue();
		}

		[Test]
		public void worker_pool_does_nothing_after_being_stopped ()
		{
			_subject.Start();
			_subject.Stop();
			_dispatcher.ClearReceivedCalls();
			_queue.ClearReceivedCalls();

			Thread.Sleep(250);
			
			_queue.DidNotReceive().TryDequeue();
		}

		[Test]
		public void if_available_flag_is_not_set_worker_waits ()
		{
			_queue.BlockUntilReady().ReturnsForAnyArgs(i => { Thread.Sleep(100000); return false; });
			Go();
			_queue.DidNotReceive().TryDequeue();
		}

		[Test]
		public void if_available_flag_is_set_worker_polls_for_work_item()
		{
			_queue.BlockUntilReady().ReturnsForAnyArgs(true);
			Go();
			_queue.Received().TryDequeue();
		}

		[Test]
		public void when_no_work_items_are_available_worker_waits_for_next_available ()
		{
			ItemAvailable(false);
			Go();

			_queue.Received().TryDequeue();
			_dispatcher.DidNotReceive().AllConsumers();
		}

		[Test]
		public void when_work_items_are_available_worker_reads_work_actions()
		{
			ItemAvailable(true);
			Go();

			_queue.Received().TryDequeue();
			_dispatcher.Received().AllConsumers();
		}
	}
}