using System;
using DispatchSharp.Internal;
using NSubstitute;
using NUnit.Framework;

namespace DispatchSharp.Unit.Tests
{
	[TestFixture]
	public class dispatch_tests
	{
		IDispatch<object> _subject;
		IWorkQueue<object> _queue;
		IWorkerPool<object> _pool;

		[SetUp]
		public void setup()
		{
			_queue = Substitute.For<IWorkQueue<object>>();
			_pool = Substitute.For<IWorkerPool<object>>();
			_subject = new Dispatch<object>(_queue, _pool);
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
		public void the_worker_pool_available_trigger_is_set_when_work_is_added ()
		{
			var thing = new object();
			_subject.AddWork(thing);
			_pool.Received().TriggerAvailable();
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

			_subject.OnExceptions(new Exception());

			Assert.That(triggered);
		}

		[Test]
		public void default_maximum_inflight_is_same_as_processor_count ()
		{
			Assert.That(_subject.MaximumInflight, Is.EqualTo(Default.ThreadCount));
		}

		[Test]
		public void maximum_inflight_can_be_set ()
		{
			int newSetting = 2;
			_subject.MaximumInflight = newSetting;
			Assert.That(_subject.MaximumInflight, Is.EqualTo(newSetting));
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