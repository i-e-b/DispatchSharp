﻿namespace DispatchSharp.Integration.Tests
{
    using System.Diagnostics;
    using DispatchSharp.QueueTypes;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture,]
	public class polling_queue_tests
	{
		IWorkQueue<string> _subject;
		IPollSource<string> _pollerSource;
		string dummy;

		[SetUp]
		public void setup()
		{
			_pollerSource = Substitute.For<IPollSource<string>>();
			_pollerSource.TryGet(out dummy).Returns(false);

			_subject = new PollingWorkQueue<string>(_pollerSource);
		}

		[Test]
		public void enqueued_data_is_consumed ()
		{
			_subject.Enqueue("paul's mum");
			var result = _subject.TryDequeue();

			Assert.That(result.HasItem);
			Assert.That(result.Item, Is.EqualTo("paul's mum"));
		}

		[Test]
		public void polled_data_is_consumed ()
		{
			_pollerSource.TryGet(out dummy).Returns(x => {
				x[0] = "phil's face";
				return true;
			});

			var result = _subject.TryDequeue();
			Assert.That(result.HasItem);
			Assert.That(result.Item, Is.EqualTo("phil's face"));
		}

		[Test]
		public void queued_data_is_used_before_polled_data ()
		{
			_subject.Enqueue("paul's mum");
			_subject.Enqueue("paul's mum");
			
			_subject.TryDequeue();
			_subject.TryDequeue();
			_subject.TryDequeue();

			_pollerSource.Received(1).TryGet(out dummy);
		}

		[Test]
		public void poller_sleeps ()
		{
			var sw = new Stopwatch();
			sw.Start();
			for (int i = 0; i < 10; i++)
			{
				_subject.TryDequeue();
			}
			sw.Stop();

			Assert.That(sw.ElapsedMilliseconds, Is.GreaterThan(800));
			Assert.That(((ISleeper)_subject).BurstSleep(), Is.GreaterThanOrEqualTo(255));
		}
	}
}