using System;
using System.Collections.Generic;
using System.Threading;
using DispatchSharp.QueueTypes;
using DispatchSharp.WorkerPools;
using NUnit.Framework;
// ReSharper disable InconsistentNaming
// ReSharper disable PossibleNullReferenceException

namespace DispatchSharp.Integration.Tests
{
    public class Threaded_Bounded_Tests: behaviours
    {
        private const int Bound = 32;
        private volatile int _maxQueueSize;
        private IWorkQueue<string>? _boundedWorkQueue;

        public override void setup()
        {
            _output = new List<string>();
            _boundedWorkQueue = new BoundedWorkQueue<string>(Bound);
            _subject = new Dispatch<string>(
                _boundedWorkQueue,
                new ThreadedWorkerPool<string>("Test"));
            _maxQueueSize = 0;
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