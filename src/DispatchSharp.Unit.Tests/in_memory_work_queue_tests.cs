using DispatchSharp.QueueTypes;
using NUnit.Framework;

namespace DispatchSharp.Unit.Tests
{
	[TestFixture]
	public class in_memory_work_queue_tests
	{
		IWorkQueue<object> _subject;

		[SetUp]
		public void setup()
		{
			_subject = new InMemoryWorkQueue<object>();
		}


		[Test]
		public void enqueued_items_can_be_dequeued()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void dequeued_items_that_have_not_been_finished_or_cancelled_cant_be_dequeued()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void dequeued_items_that_have_been_finished_cant_be_dequeued()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void dequeued_items_that_have_been_cancelled_can_be_dequeued_again()
		{
			Assert.Inconclusive();
		}
	}
}