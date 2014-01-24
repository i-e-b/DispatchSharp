using System;
using System.Collections.Generic;
using System.Threading;
using DispatchSharp.QueueTypes;
using DispatchSharp.WorkerPools;
using NUnit.Framework;

namespace DispatchSharp.Integration.Tests
{
    public class Direct_Bounded_tests: Direct_InMemory_tests
    {
        private const int Bound = 32;
        private IWorkQueue<string> _boundedWorkQueue;
        private volatile int _maxQueueSize;

        public override void setup()
        {
            _output = new List<string>();
            _boundedWorkQueue = new BoundedWorkQueue<string>(Bound);
            _subject = new Dispatch<string>(
                _boundedWorkQueue,
                new DirectWorkerPool<string>());
        }

        [Test]
        public void queue_size_is_never_exceeded()
        {
            _subject.AddConsumer(DelayedConsumer);
            _subject.Start();
            for (var i = 0; i < Bound * 2; i++)
            {
                _subject.AddWork("hello");
            }
            _subject.WaitForEmptyQueueAndStop();
            Assert.That(_maxQueueSize, Is.AtMost(Bound));
        }

        private void DelayedConsumer(string obj)
        {
            Thread.Sleep(10);
            _maxQueueSize = Math.Max(_boundedWorkQueue.Length(), _maxQueueSize);
        }
    }
}