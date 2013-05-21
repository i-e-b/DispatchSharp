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
			var source = "Hello, please mind the gap";
			_subject.Enqueue(source);
			var result = _subject.TryDequeue();

			Assert.That(result.HasItem);
			Assert.That(result.Item, Is.EqualTo(source));
		}

		[Test]
		public void dequeued_items_that_have_not_been_finished_or_cancelled_cant_be_dequeued()
		{
			var source = "Hello, please mind the gap";
			_subject.Enqueue(source);
			var dummy = _subject.TryDequeue();
			var result = _subject.TryDequeue();

			Assert.True(dummy.HasItem);
			Assert.False(result.HasItem);
		}

		[Test]
		public void dequeued_items_that_have_been_finished_cant_be_dequeued()
		{
			var source = "Hello, please mind the gap";
			_subject.Enqueue(source);
			_subject.TryDequeue().Finish();
			var result = _subject.TryDequeue();

			Assert.False(result.HasItem);
		}

		[Test]
		public void dequeued_items_that_have_been_cancelled_can_be_dequeued_again()
		{
			var source = "Hello, please mind the gap";
			_subject.Enqueue(source);
			_subject.TryDequeue().Cancel();
			var result = _subject.TryDequeue();

			Assert.True(result.HasItem);
			Assert.That(result.Item, Is.EqualTo(source));
		}

		[Test]
		public void queue_length_represents_number_of_items_on_the_queue ()
		{
			for (int i = 0; i < 10; i++)
			{
				Assert.That(_subject.Length(), Is.EqualTo(i));
				_subject.Enqueue(new object());
			}

			for (int i = 9; i >= 0; i--)
			{
				_subject.TryDequeue();
				Assert.That(_subject.Length(), Is.EqualTo(i));
			}
		}
	}
}