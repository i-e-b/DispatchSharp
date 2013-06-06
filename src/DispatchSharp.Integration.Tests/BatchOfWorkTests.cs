using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DispatchSharp.Integration.Tests
{
	[TestFixture]
	public class BatchOfWorkTests
	{
		List<string> _work;
		List<string> _output;
		List<string> _expected;
		List<string> _exceptions;

		[SetUp]
		public void setup()
		{
			_work = new List<string> { "A", "B", "C", "THROW"};
			_output = new List<string>();
			_exceptions = new List<string>();
			_expected = new List<string> { "Hello, A", "Hello, B", "Hello, C" };
		}

		[Test]
		public void single_batch_of_work_completes_all_items()
		{
			var dispatcher = Dispatch<string>.CreateDefaultMultithreaded("MyTask");

			dispatcher.AddConsumer(Action);
			dispatcher.Exceptions += (s,e) => _exceptions.Add(e.SourceException.Message);
			dispatcher.Start();
			dispatcher.AddWork(_work);
			dispatcher.WaitForEmptyQueueAndStop();

			Assert.That(_output, Is.EquivalentTo(_expected));
			Assert.That(_exceptions, Is.EquivalentTo(new [] {"Yo!"}));
		}


		[Test]
		public void batch_helper_completes_work_and_calls_back_to_exception_handler ()
		{
			Dispatch<string>.ProcessBatch("MyBatch", _work, Action, 1, e => _exceptions.Add(e.Message));
			
			Assert.That(_output, Is.EquivalentTo(_expected));
			Assert.That(_exceptions, Is.EquivalentTo(new [] {"Yo!"}));
		}

		void Action(string str)
		{
			if (str == "THROW")
				throw new Exception("Yo!");
			_output.Add("Hello, " + str);
		}
	}
}