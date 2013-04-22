using System;
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
		public void pool_is_started_when_the_first_worker_is_supplied()
		{
			_pool.DidNotReceive().Start();
			_subject.AddConsumer(o => { });
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

			Assert.That(_subject.WorkActions(), Is.EquivalentTo(new []{a,b}));
		}

		[Test]
		public void exception_trigger_fires_exceptions_event ()
		{
			var triggered = false;
			_subject.Exceptions += (a,b)=> { triggered = true; };

			_subject.OnExceptions(new Exception());

			Assert.That(triggered);
		}
	}
}