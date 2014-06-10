using System.Threading;
using DispatchSharp.Internal;
using DispatchSharp.QueueTypes;
using NSubstitute;
using NUnit.Framework;

namespace DispatchSharp.Unit.Tests
{
    [TestFixture, Category(Categories.FastTests)]
    public class bounded_work_queue_item_tests
    {
        private IWorkQueueItem<object> _subject;
        private IWorkQueueItem<object> _item;
        private IWaitHandle _waitHandle;

        [SetUp]
        public void setup()
        {
            _item = Substitute.For<IWorkQueueItem<object>>();
            _waitHandle = Substitute.For<IWaitHandle>();
            _subject = new BoundedWorkQueueItem<object>(_item, _waitHandle);
        }

        [Test]
        public void calls_to_has_item_are_delegated()
        {
            _item.HasItem.Returns(true);

            var hasItem = _subject.HasItem;
            Assert.True(hasItem);
        }

        [Test]
        public void calls_to_item_are_delegated()
        {
            var expected = new object();
            _item.Item.Returns(expected);

            var actual = _subject.Item;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void calls_to_finish_are_delegated()
        {
            _subject.Finish();

            _item.Received().Finish();
        }

        [Test]
        public void calls_to_cancel_are_delegated()
        {
            _subject.Cancel();

            _item.Received().Cancel();
        }

        [Test]
        public void calls_to_cancel_wait()
        {
            _subject.Cancel();

            _waitHandle.Received().WaitOne();
        }

        [Test]
        public void multiple_cancels_are_waited_once()
        {
            _subject.Cancel();
            _subject.Cancel();

            _waitHandle.Received(1).WaitOne();
            _item.Received(2).Cancel();
        }

        [Test]
        public void multiple_cancels_are_waited_once_multithread()
        {
            var mre = new ManualResetEventSlim(false);

            var ce = new CountdownEvent(2);
            DoCancel(mre, ce);
            DoCancel(mre, ce);

            mre.Set();
            ce.Wait();

            _waitHandle.Received(1).WaitOne();
            _item.Received(2).Cancel();
        }

        private void DoCancel(ManualResetEventSlim manualResetEventSlim, CountdownEvent countdownEvent)
        {
            var thread = new Thread(() =>
                                        {
                                            manualResetEventSlim.Wait();
                                            Thread.Sleep(0);
                                            _subject.Cancel();
                                            countdownEvent.Signal();
                                        });
            thread.Start();
        }
    }
}