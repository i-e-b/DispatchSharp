using System;
using System.Diagnostics;
using System.Threading;
using DispatchSharp.QueueTypes;
using NSubstitute;
using NUnit.Framework;
// ReSharper disable InconsistentNaming
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException

namespace DispatchSharp.Unit.Tests
{
    [TestFixture, Category(Categories.FastTests)]
    public class bounded_work_queue_tests
    {
        private IWorkQueue<object>? _subject;
        private IWorkQueue<object>? _mockQueue;
        private const int Bound = 8;

        [SetUp]
        public void setup()
        {
            _mockQueue = Substitute.For<IWorkQueue<object>>();
            _subject = new BoundedWorkQueue<object>(_mockQueue, Bound);
        }

        [Test]
        public void delegates_enqueue()
        {
            var item = new object();
            _subject.Enqueue(item);
            _mockQueue.Received().Enqueue(item);
        }

        [Test]
        public void delegate_try_dequeue()
        {
            _subject.TryDequeue();
            _mockQueue.Received().TryDequeue();
        }

        [Test]
        public void delegates_length()
        {
            const int expectedItem = default(int);
            _mockQueue.Length().Returns(expectedItem);

            var actualItem = _subject.Length();
            _mockQueue.Received().Length();
            Assert.That(actualItem, Is.EqualTo(expectedItem));
        }

        [Test]
        public void delegates_block_until_ready()
        {
            const bool expectedItem = default(bool);
            _mockQueue.BlockUntilReady().Returns(expectedItem);

            var actualItem = _subject.BlockUntilReady();
            _mockQueue.Received().BlockUntilReady();
            Assert.That(actualItem, Is.EqualTo(expectedItem));
        }

        [Test]
        public void waits_until_successful_dequeue_to_continue()
        {
            var qwi = Substitute.For<IWorkQueueItem<object>>();
            qwi.HasItem.Returns(true);
            _mockQueue.TryDequeue().Returns(qwi);

            for (var i = 0; i < Bound; i++)
            {
                _subject.Enqueue(new object());
            }


            var sw = Stopwatch.StartNew();
            DequeueIn100Ms();
            _subject.Enqueue(new object());
            sw.Stop();

            Assert.That(sw.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(100));
        }

        [Test]
        public void dequeued_items_that_have_been_cancelled_can_be_dequeued_again()
        {
            var source = "Hello, please mind the gap";
            var item = Substitute.For<IWorkQueueItem<object>>();
            item.HasItem.Returns(true);
            item.Item.Returns(source);

            _mockQueue.TryDequeue().Returns(item);

            _subject.Enqueue(source);
            _subject.TryDequeue().Cancel();
            var result = _subject.TryDequeue();

            Assert.True(result.HasItem);
            Assert.That(result.Item, Is.EqualTo(source));
        }


        [Test, Timeout(1000)]
        public void enqueue_is_exception_safe()
        {
            _subject = new BoundedWorkQueue<object>(_mockQueue, 1);

            var objectToThrowAt = new object();
            _mockQueue.When(queue => queue.Enqueue(objectToThrowAt))
                      .Do(_ => throw new DummyException());

            try
            {
                _subject.Enqueue(objectToThrowAt);
            }
            catch (DummyException)
            {
            }

            _subject.Enqueue(new object());
        }

        private void DequeueIn100Ms()
        {
            var t = new Thread(() =>
                                   {
                                       Thread.Sleep(100);
                                       _subject.TryDequeue();
                                   });
            t.Start();
        }

        private class DummyException : Exception
        {
        }
    }
}