using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
// ReSharper disable PossibleNullReferenceException
// ReSharper disable StringIndexOfIsCultureSpecific.1

namespace DispatchSharp.Integration.Tests
{
    [TestFixture]
    public class WorkAndConsumerOrdering
    {
        [Test]
        [Repeat(3)]
        public void work_parallel_should_consume_work_in_parallel_and_issue_consumers_in_sequence()
        {
            var output = new List<string>();
            var lockObj = new object();
            var subject = Dispatch<string>.CreateWorkParallelMultiThreaded("UnitTest");
            
            subject.AddConsumer(w => {
                lock (lockObj) { output.Add($"c1,w{w}"); }
                Thread.Sleep(100);
            });
            subject.AddConsumer(w => {
                lock (lockObj) { output.Add($"c2,w{w}"); }
                Thread.Sleep(200);
            });
            subject.AddConsumer(w => {
                lock (lockObj) { output.Add($"c3,w{w}"); }
                Thread.Sleep(300);
            });

            for (int i = 0; i < 5; i++)
            {
                subject.AddWork((i+1).ToString());
            }
            
            Assert.That(subject.CurrentQueued(), Is.EqualTo(5), "queue items");
            Assert.That(subject.AllConsumers().Count(), Is.EqualTo(3), "consumer count");
            
            subject.Start();
            subject.WaitForEmptyQueueAndStop();
            
            
            Assert.That(output.Count, Is.EqualTo(5*3), "items processed");
            
            // Check that each consumer is in sequence
            var order = string.Join("; ", output);
            Assert.That(order.IndexOf("c1,w1"), Is.LessThan(order.IndexOf("c2,w1")));
            Assert.That(order.IndexOf("c1,w2"), Is.LessThan(order.IndexOf("c2,w2")));
            Assert.That(order.IndexOf("c1,w3"), Is.LessThan(order.IndexOf("c2,w3")));
            Assert.That(order.IndexOf("c1,w4"), Is.LessThan(order.IndexOf("c2,w4")));
            Assert.That(order.IndexOf("c1,w5"), Is.LessThan(order.IndexOf("c2,w5")));
            
            Assert.That(order.IndexOf("c2,w1"), Is.LessThan(order.IndexOf("c3,w1")));
            Assert.That(order.IndexOf("c2,w2"), Is.LessThan(order.IndexOf("c3,w2")));
            Assert.That(order.IndexOf("c2,w3"), Is.LessThan(order.IndexOf("c3,w3")));
            Assert.That(order.IndexOf("c2,w4"), Is.LessThan(order.IndexOf("c3,w4")));
            Assert.That(order.IndexOf("c2,w5"), Is.LessThan(order.IndexOf("c3,w5")));
        }
        
        
        [Test]
        [Repeat(3)]
        public void task_parallel_should_consume_work_in_order_and_issue_consumers_in_parallel()
        {
            var output = new List<string>();
            var lockObj = new object();
            var subject = Dispatch<string>.CreateTaskParallelMultiThreaded("UnitTest", threadCount: 4);
            
            subject.AddConsumer(w => {
                lock (lockObj) { /*Console.Write(" c1!");*/ output.Add($"c1,w{w}"); }
                Thread.Sleep(100);
            });
            subject.AddConsumer(w => {
                lock (lockObj) { /*Console.Write(" c2!");*/ output.Add($"c2,w{w}"); }
                Thread.Sleep(200);
            });
            subject.AddConsumer(w => {
                lock (lockObj) { /*Console.Write(" c3!");*/ output.Add($"c3,w{w}"); }
                Thread.Sleep(300);
            });

            for (int i = 0; i < 5; i++)
            {
                subject.AddWork((i+1).ToString());
            }
            
            Assert.That(subject.CurrentQueued(), Is.EqualTo(5), "queue items");
            Assert.That(subject.AllConsumers().Count(), Is.EqualTo(3), "consumer count");
            
            subject.Start();
            subject.WaitForEmptyQueueAndStop();
            
            Console.WriteLine(string.Join("; ", output));
            
            Assert.That(output.Count, Is.EqualTo(5*3), "items processed");
            
            // Check that each task is in sequence
            var order = string.Join("; ", output);
            Assert.That(order.IndexOf("c1,w1"), Is.LessThan(order.IndexOf("c1,w2")));
            Assert.That(order.IndexOf("c1,w2"), Is.LessThan(order.IndexOf("c1,w3")));
            Assert.That(order.IndexOf("c1,w3"), Is.LessThan(order.IndexOf("c1,w4")));
            Assert.That(order.IndexOf("c1,w4"), Is.LessThan(order.IndexOf("c1,w5")));
            
            Assert.That(order.IndexOf("c2,w1"), Is.LessThan(order.IndexOf("c2,w2")));
            Assert.That(order.IndexOf("c2,w2"), Is.LessThan(order.IndexOf("c2,w3")));
            Assert.That(order.IndexOf("c2,w3"), Is.LessThan(order.IndexOf("c2,w4")));
            Assert.That(order.IndexOf("c2,w4"), Is.LessThan(order.IndexOf("c2,w5")));
            
            Assert.That(order.IndexOf("c3,w1"), Is.LessThan(order.IndexOf("c3,w2")));
            Assert.That(order.IndexOf("c3,w2"), Is.LessThan(order.IndexOf("c3,w3")));
            Assert.That(order.IndexOf("c3,w3"), Is.LessThan(order.IndexOf("c3,w4")));
            Assert.That(order.IndexOf("c3,w4"), Is.LessThan(order.IndexOf("c3,w5")));
        }
        
        // TODO:
        // - All work items should be finished once
    }
}