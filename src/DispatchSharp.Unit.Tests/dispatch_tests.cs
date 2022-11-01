using System;
using DispatchSharp.QueueTypes;
using NSubstitute;
using NUnit.Framework;
// ReSharper disable InconsistentNaming
// ReSharper disable PossibleNullReferenceException
// ReSharper disable AssignNullToNotNullAttribute

namespace DispatchSharp.Unit.Tests
{
    [TestFixture, Category(Categories.FastTests)]
	public class dispatch_tests
	{
		IDispatch<object>? _subject;
		IWorkQueue<object>? _queue;
		IWorkerPool<object>? _pool;

		[SetUp]
		public void setup()
		{
			_queue = Substitute.For<IWorkQueue<object>>();
			_pool = Substitute.For<IWorkerPool<object>>();
			_pool.PoolSize().Returns(10000);
			_subject = new Dispatch<object>(_queue, _pool);
		}

		[Test]
		public void default_inflight_limit_is_same_as_cpu_core_count ()
		{
			Assert.That(_subject.MaximumInflight(), Is.EqualTo(Environment.ProcessorCount));
		}

		[Test]
		public void pool_is_not_started_when_the_first_worker_is_supplied()
		{
			_pool.DidNotReceive().Start();
			_subject.AddConsumer(o => { });
			_pool.DidNotReceive().Start();
		}

		[Test]
		public void if_pool_is_started_with_no_consumers_an_exception_is_thrown()
		{
			_pool.DidNotReceive().Start();
			var ex = Assert.Throws<InvalidOperationException>(()=>_subject.Start());
			Assert.That(ex.Message, Is.StringContaining("A dispatcher can't be started until it has at least one consumer"));
		}

		[Test]
		public void pool_is_started_when_dispatcher_is_started()
		{
			_pool.DidNotReceive().Start();
			_subject.AddConsumer(o => { });
			_subject.Start();
			_pool.Received().Start();
		}

		[Test]
		public void pool_is_stopped_when_dispatcher_is_stopped ()
		{
			_subject.Stop();
			_pool.Received().Stop();
		}

		[Test]
		public void added_work_is_passed_to_queue ()
		{
			var thing = new object();
			_subject.AddWork(thing);
			_queue.Received().Enqueue(thing);
		}

		[Test]
		public void added_work_enumerables_are_all_added_to_queue ()
		{
			var thing = new []{new object(), new object(), new object()};
			_subject.AddWork(thing);
			_queue.Received(3).Enqueue(Arg.Any<object>());
		}

		[Test]
		public void waiting_stop_polls_queue_and_eventually_stops ()
		{
			_queue.Length().Returns(3,2,1,0);

			_subject.WaitForEmptyQueueAndStop();

			_queue.Received(4).Length();
			_pool.Received().Stop();
		}

		[Test]
		public void added_workers_are_available_to_enumerate ()
		{
			Action<object> a = o => { }, b = o => { };

			_subject.AddConsumer(a);
			_subject.AddConsumer(b);

			Assert.That(_subject.AllConsumers(), Is.EquivalentTo(new []{a,b}));
		}

		[Test]
		public void exception_trigger_fires_exceptions_event ()
		{
			var triggered = false;
			_subject.Exceptions += (a,b)=> { triggered = true; };

			((IDispatchInternal<object>)_subject).OnExceptions(new Exception(), new WorkQueueItem<object>(null, o => { }, o => { }));

			Assert.That(triggered);
		}

		[Test]
		public void maximum_inflight_can_be_set ()
		{
			int newSetting = 2;
			_subject.SetMaximumInflight(newSetting);
			Assert.That(_subject.MaximumInflight(), Is.EqualTo(newSetting));
		}

		[Test]
		public void inflight_is_read_from_worker_pool ()
		{
			var expected = 14;
			_pool.WorkersInflight().Returns(expected);
			_pool.DidNotReceive().WorkersInflight();
			var actual = _subject.CurrentInflight();

			_pool.Received().WorkersInflight();
			Assert.That(actual, Is.EqualTo(expected));
		}
	}
}