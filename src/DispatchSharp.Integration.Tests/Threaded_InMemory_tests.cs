using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DispatchSharp.QueueTypes;
using NUnit.Framework;

namespace DispatchSharp.Integration.Tests
{
	[TestFixture]
	public class Threaded_InMemory_tests : behaviours
	{
		[SetUp]
		public override void setup()
		{
			_output = new List<string>();
			_subject = new Dispatch<string>(new InMemoryWorkQueue<string>(), new ThreadedWorkerPool<string>("Test", 2));
		}
		
		[Test]
		public void stopping_the_dispatcher_completes_all_current_actions_before_stopping()
		{
			_subject.AddConsumer(s =>
			{
				_output.Add("Start");
				Thread.Sleep(2000);
				_output.Add("End");
			});
			_subject.Start();

			for (int i = 0; i < 100; i++) { _subject.AddWork(""); }

			Thread.Sleep(1500);

			_subject.Stop();

			Assert.That(_output.Count(), Is.GreaterThan(0));
			Assert.That(_output.Count(s=>s=="Start"), Is.EqualTo(_output.Count(s=>s=="End")));
		}
	}
}
