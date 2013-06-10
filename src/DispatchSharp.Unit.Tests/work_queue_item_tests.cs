using System.Collections.Generic;
using DispatchSharp.QueueTypes;
using NUnit.Framework;

namespace DispatchSharp.Unit.Tests
{
	[TestFixture]
	public class work_queue_item_tests
	{
		IWorkQueueItem<string> _subject;
		List<string> _finished, _cancelled;

		[SetUp]
		public void setup()
		{
			_finished = new List<string>();
			_cancelled = new List<string>();
			_subject = new WorkQueueItem<string>("data", s => _finished.Add(s), s => _cancelled.Add(s));
		}

		[Test]
		public void calling_cancel_on_the_item_calls_the_cancel_delegate ()
		{
			_subject.Cancel();

			Assert.That(_cancelled, Contains.Item("data"));
		}

		[Test]
		public void calling_finish_on_the_item_calls_the_finish_delegate ()
		{
			_subject.Finish();

			Assert.That(_finished, Contains.Item("data"));
		}

		[Test]
		public void if_finish_is_called_all_other_calls_to_finish_and_cancel_are_ignored()
		{
			_subject.Finish();

			for (int i = 0; i < 5; i++)
			{
				_subject.Cancel();
				_subject.Finish();
			}

			Assert.That(_finished, Is.EquivalentTo(new[] { "data" }));
			Assert.That(_cancelled, Is.Empty);
		}

		[Test]
		public void if_cancel_is_called_all_other_calls_to_finish_and_cancel_are_ignored()
		{
			_subject.Cancel();

			for (int i = 0; i < 5; i++)
			{
				_subject.Finish();
				_subject.Cancel();
			}

			Assert.That(_cancelled, Is.EquivalentTo(new[] { "data" }));
			Assert.That(_finished, Is.Empty);
		}

	}
}