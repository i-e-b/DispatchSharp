﻿namespace DispatchSharp.Integration.Tests
{
    using System;
    using System.Threading;
    using DispatchSharp.WorkerPools;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
	public class threaded_worker_pool_action_tests
	{
		
		IDispatch<object> _dispatcher;
		IWorkerPool<object> _subject;
		IWorkQueue<object> _queue;
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