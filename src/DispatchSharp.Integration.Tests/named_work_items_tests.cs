using System.Linq;
using DispatchSharp.QueueTypes;
using DispatchSharp.WorkerPools;
using NUnit.Framework;
// ReSharper disable InconsistentNaming

namespace DispatchSharp.Integration.Tests
{
    [TestFixture]
    public class named_work_items_tests
    {
        [Test]
        public void work_items_can_be_given_individual_names_and_the_names_of_active_items_can_be_queried()
        {
		    
            IDispatch<TestWorkItem> subject = new Dispatch<TestWorkItem>(
                new InMemoryWorkQueue<TestWorkItem>(),
                new DirectWorkerPool<TestWorkItem>());
            subject.AddConsumer(TestHandler);
		    
            subject.AddWork(new TestWorkItem()); // no name, should not be listed
            subject.AddWork(new TestWorkItem(), name: "Single Task"); // named single item
            subject.AddWork(new []{new TestWorkItem(), new TestWorkItem() }, name:"Multi-task"); // group each with same name
            subject.AddWork(new []{new TestWorkItem(), new TestWorkItem() }); // group with no name
		    
            var listBefore = subject.ListNamedTasks().ToList();
            Assert.That(listBefore.Count, Is.EqualTo(3), "count of named items");
            Assert.That(listBefore.Count(n=>n=="Single Task"), Is.EqualTo(1), "single item appears once");
            Assert.That(listBefore.Count(n=>n=="Multi-task"), Is.EqualTo(2), "multi items appear once per queued item");
		    
            var queueLength = subject.CurrentQueued();
            Assert.That(queueLength, Is.EqualTo(6), "count of all items");
		    
            subject.Start();
		    
            // work queue should now be clear
            var listAfter = subject.ListNamedTasks().ToList();
            Assert.That(listAfter.Count, Is.EqualTo(0), "count of named items after processing");
        }

        private void TestHandler(TestWorkItem obj)
        {
            // no-op
        }

        private class TestWorkItem
        {
        }
    }
}