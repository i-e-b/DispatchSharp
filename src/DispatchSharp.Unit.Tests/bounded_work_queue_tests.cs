using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DispatchSharp.QueueTypes;
using NSubstitute;
using NUnit.Framework;

namespace DispatchSharp.Unit.Tests
{
    public class bounded_work_queue_tests
    {
        private IWorkQueue<object> _subject;
        private IWorkQueue<object> _mockQueue;
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
            var expectedItem = Substitute.For<IWorkQueueItem<object>>();
            _mockQueue.TryDequeue().Returns(expectedItem);

            var actualItem = _subject.TryDequeue();
            _mockQueue.Received().TryDequeue();
            Assert.That(actualItem, Is.EqualTo(expectedItem));
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

        [Test, Timeout(1000)]
        public void enqueue_is_exception_safe()
        {
            _subject = new BoundedWorkQueue<object>(_mockQueue, 1);

            var objectToThrowAt = new object();
            _mockQueue.When(queue => queue.Enqueue(objectToThrowAt))
                      .Do(_ => { throw new DummyException(); });

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