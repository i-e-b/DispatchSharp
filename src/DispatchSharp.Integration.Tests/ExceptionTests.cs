using System;
using NUnit.Framework;

namespace DispatchSharp.Integration.Tests
{
	[TestFixture]
	public class ExceptionTests
	{
		IDispatch<object> _subject;
		int _calls;

		[SetUp]
		public void setup()
		{
			_calls = 0;
			_subject = Dispatch<object>.CreateDefaultMultiThreaded("test");
			_subject.Exceptions += (s,e) => {
				if (_calls++ < 2)
				{
					e.WorkItem.Cancel();
				}
				// if we don't cancel, the work item will be finished by default.
			};
		}

		[Test]
		public void can_cancel_and_retry_work_items_using_the_event_handler ()
		{
			_subject.AddConsumer(o => { throw new Exception("busted"); });
			_subject.AddWork(new object());
			_subject.Start();
			_subject.WaitForEmptyQueueAndStop();

			Assert.That(_calls, Is.EqualTo(3));
		}
	}

}