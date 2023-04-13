// ReSharper disable InconsistentNaming
// ReSharper disable PossibleNullReferenceException
// ReSharper disable AssignNullToNotNullAttribute

using System.Linq;
using DispatchSharp.QueueTypes;

namespace DispatchSharp.Integration.Tests
{
    using System;
    using System.Threading;
    using WorkerPools;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class named_work_items_tests
    {
	    [Test]
	    public void work_items_can_be_given_individual_names_and_the_names_of_active_items_can_be_queried()
	    {
		    
		    var subject = new Dispatch<TestWorkItem>(
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

    [TestFixture]
	public class threaded_worker_pool_action_tests
	{
		
		IDispatch<object>? _dispatcher;
		IWorkerPool<object>? _subject;
		IWorkQueue<object>? _queue;
		volatile bool _actionCalled;

		[SetUp]
		public void setup ()
		{
			_actionCalled = false;
			_dispatcher = Substitute.For<IDispatch<object>>();
			_dispatcher.MaximumInflight().Returns(1);
			_queue = Substitute.For<IWorkQueue<object>>();
			_subject = new ThreadedWorkerPool<object>("name");
			_subject.SetSource(_dispatcher, _queue);
		}

		[Test]
		public void a_successful_action_results_in_item_completion ()
		{
			ForSuccessfulAction();
			var item = ItemAvailable();
			Go();

			Assert.That(_actionCalled);
			item.Received().Finish();
		}

		[Test]
		public void a_failing_action_results_in_item_being_finished ()
		{
			ForFailingAction();
			var item = ItemAvailable();
			Go();

			Assert.That(_actionCalled);
			item.Received().Finish();
		}

		void ForSuccessfulAction()
		{
			_dispatcher.AllConsumers().Returns(
				new Action<object>[]{
					o => {_actionCalled = true; }
				}
				);
		}
		void ForFailingAction()
		{
			_dispatcher.AllConsumers().Returns(
				new Action<object>[]{
					o => {
						_actionCalled = true;
						if (_actionCalled) throw new Exception();
					}
				}
				);
		}
		IWorkQueueItem<object> ItemAvailable()
		{
			var item = Substitute.For<IWorkQueueItem<object>>();
			item.HasItem.Returns(true);
			_queue.TryDequeue().Returns(item);
			return item;
		}
		void Go()
		{
			_subject.Start();
			Thread.Sleep(20);
			_subject.Stop();
		}
	}
}