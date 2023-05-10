using System.Diagnostics;
using System.Threading;
using DispatchSharp.WorkerPools;
using NSubstitute;
using NUnit.Framework;
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException
// ReSharper disable InconsistentNaming

namespace DispatchSharp.Unit.Tests
{
    [TestFixture, Category(Categories.FastTests)]
	public class threaded_worker_pool_tests:threaded_worker_pool_base
	{
		[SetUp]
		public void setup ()
		{
			_dispatcher = Substitute.For<IDispatch<object>>();
			_dispatcher.MaximumInflight().Returns(4);
			_queue = Substitute.For<IWorkQueue<object>>();
			_subject = new ThreadedWorkerPool<object>("name");
			_subject.SetSource(_dispatcher, _queue);
		}

		[Test]
		public void threaded_worker_pool_starts_with_no_worker_threads ()
		{
			var withDefaults = new ThreadedWorkerPool<object>("name");
			Assert.That(withDefaults.PoolSize(), Is.EqualTo(0));
		}

		[Test]
		public void worker_pool_does_nothing_if_not_started ()
		{
			Thread.Sleep(100);
			_queue.DidNotReceive().TryDequeue();
		}

		[Test]
		public void worker_pool_does_nothing_after_being_stopped ()
		{
			_subject.Start();
			_subject.Stop();
			_dispatcher.ClearReceivedCalls();
			_queue.ClearReceivedCalls();

			Thread.Sleep(100);
			
			_queue.DidNotReceive().TryDequeue();
		}

		[Test]
		public void if_available_flag_is_not_set_worker_waits_and_aborts_if_still_waiting ()
		{
			var sw = new Stopwatch();
			sw.Start();

			_queue.BlockUntilReady().ReturnsForAnyArgs(i => { Thread.Sleep(1000000); return QueueState.HasItems; });
			Go();
			_queue.DidNotReceive().TryDequeue();

			sw.Stop();
			Assert.That(sw.ElapsedMilliseconds, Is.LessThan(1000));
		}

		[Test]
		public void if_available_flag_is_set_worker_polls_for_work_item()
		{
			_queue.BlockUntilReady().ReturnsForAnyArgs(QueueState.Unknown);
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