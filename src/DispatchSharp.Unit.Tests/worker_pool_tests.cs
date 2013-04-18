using System.Threading;
using NSubstitute;
using NUnit.Framework;

namespace DispatchSharp.Unit.Tests
{
	[TestFixture]
	public class worker_pool_tests
	{
		IDispatch<object> _dispatcher;
		WorkerPool<object> _subject;
		IWorkQueue<object> _queue;

		[SetUp]
		public void setup ()
		{
			_dispatcher = Substitute.For<IDispatch<object>>();
			_queue = Substitute.For<IWorkQueue<object>>();
			_subject = new WorkerPool<object>("name", 1);
			_subject.SetSource(_dispatcher, _queue);
		}

		[Test]
		public void worker_requests_available_flag_of_dispatcher ()
		{
			_subject.Start();
			Thread.Sleep(250);
			_subject.Stop();

			_dispatcher.Available.Received().WaitOne();
		}
	}
}