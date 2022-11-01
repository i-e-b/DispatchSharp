using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
// ReSharper disable PossibleNullReferenceException
// ReSharper disable AssignNullToNotNullAttribute

namespace DispatchSharp.Integration.Tests
{
	[TestFixture]
	public class BatchOfWorkTests
	{
		List<string>? _work;
		List<string>? _output;
		List<string>? _expected;
		List<string>? _exceptions;

		[SetUp]
		public void setup()
		{
			_work = new List<string> { "A", "B", "C", "THROW"};
			_output = new List<string>();
			_exceptions = new List<string>();
			_expected = new List<string> { "Hello, A", "Hello, B", "Hello, C" };
		}

		[Test]
		[Repeat(3)]
		public void single_batch_of_work_completes_all_items()
		{
			var dispatcher = Dispatch<string>.CreateDefaultMultiThreaded("MyTask");

			dispatcher.AddConsumer(Action);
			dispatcher.Exceptions += (s,e) => _exceptions.Add(e.SourceException.Message);
			dispatcher.AddWork(_work);
			dispatcher.Start();
			dispatcher.WaitForEmptyQueueAndStop();

			Assert.That(_output, Is.EquivalentTo(_expected));
			Assert.That(_exceptions, Is.EquivalentTo(new [] {"Yo!"}));
		}

		[Test]
		public void wait_for_stop_respects_timeout ()
		{
			var dispatcher = Dispatch<string>.CreateDefaultMultiThreaded("MyTask");

			dispatcher.SetMaximumInflight(1);
			dispatcher.AddConsumer(SlowAction); // one second per job
			for (int i = 0; i < 100; i++)
			{
				dispatcher.AddWork(_work);
			}
			dispatcher.Start();


			var sw = new Stopwatch();
			sw.Start();
			dispatcher.WaitForEmptyQueueAndStop(TimeSpan.FromSeconds(2));
			sw.Stop();

			Assert.That(sw.Elapsed, Is.GreaterThanOrEqualTo(TimeSpan.FromSeconds(2.0)));
			Assert.That(sw.Elapsed, Is.LessThanOrEqualTo(TimeSpan.FromSeconds(3.1)));
		}



		[Test]
		public void batch_helper_completes_work_and_calls_back_to_exception_handler ()
		{
			Dispatch<string>.ProcessBatch("MyBatch", _work, Action, 1, e => _exceptions.Add(e.Message));
			
			Assert.That(_output, Is.EquivalentTo(_expected));
			Assert.That(_exceptions, Is.EquivalentTo(new [] {"Yo!"}));
		}

		void SlowAction(string obj)
		{
			Thread.Sleep(1000);
		}


		void Action(string str)
		{
			if (str == "THROW")
				throw new Exception("Yo!");
			_output.Add("Hello, " + str);
		}
	}
}