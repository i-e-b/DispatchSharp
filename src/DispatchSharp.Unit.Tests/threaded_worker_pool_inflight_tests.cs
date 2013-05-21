using NSubstitute;
using NUnit.Framework;

namespace DispatchSharp.Unit.Tests
{
	[TestFixture]
	public class threaded_worker_pool_inflight_tests:threaded_worker_pool_base
	{
		[SetUp]
		public void setup ()
		{
			_dispatcher = Substitute.For<IDispatch<object>>();

			_queue = Substitute.For<IWorkQueue<object>>();
			_subject = new ThreadedWorkerPool<object>("name", 4);
			_subject.SetSource(_dispatcher, _queue);
		}
		 
		[Test]
		public void if_dispatcher_maximum_inflight_is_less_than_one_no_tasks_will_be_started ()
		{
			_dispatcher.MaximumInflight.Returns(0);
			Available(true);
			Go();
			_queue.DidNotReceive().TryDequeue();
		}

		[Test]
		public void if_maximum_inflight_is_greater_than_zero_tasks_will_be_started ()
		{
			_dispatcher.MaximumInflight.Returns(1);
			Available(true);
			Go();
			_queue.Received().TryDequeue();
		}
	}
}