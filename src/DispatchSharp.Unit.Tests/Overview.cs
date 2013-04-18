using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DispatchSharp.QueueTypes;
using NUnit.Framework;

namespace DispatchSharp.Unit.Tests
{
	[TestFixture]
	public class Overview
	{
		IDispatch<string> _subject;
		List<string> _output;
		readonly Random _random = new Random();

		[SetUp]
		public void setup()
		{
			_output = new List<string>();
			_subject = new Dispatch<string>(new InMemoryWorkQueue<string>(), new WorkerPool<string>("Test", 2));
		}

		[Test]
		public void fuzzing_test ()
		{
			var input = RandomStrings(1000);
			_subject.AddConsumer(s => _output.Add(s));
			foreach (var str in input) { 
				Thread.Sleep(_random.Next(0,10));
				_subject.AddWork(str);
			}

			_subject.Stop();
			Assert.That(_output.Count, Is.EqualTo(1000));
		}

		[Test]
		public void consumers_process_all_added_work()
		{
			_subject.AddConsumer(s =>
			{
				Console.WriteLine(s);
				_output.Add(s);
			});

			_subject.AddWork("Hello");
			_subject.AddWork("World");

			Thread.Sleep(1000);
			Assert.That(_output, Is.EquivalentTo(new[] { "Hello", "World" }));
		}

		[Test]
		public void a_consumer_that_throws_an_exception_fires_an_event_but_does_not_stop_the_worker ()
		{
			_subject.AddConsumer(s =>
			{
				if (s == "THROW") throw new Exception("Woggle");
				Console.WriteLine(s);
				_output.Add(s);
			});

			_subject.Exceptions += (e, ex) => _output.Add("Wiggle" + ex.SourceException.Message);

			_subject.AddWork("Hello");
			_subject.AddWork("THROW");
			_subject.AddWork("World");
			Thread.Sleep(1000);
			Assert.That(_output, Is.EquivalentTo(new[] { "Hello", "WiggleWoggle", "World" }));
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

			for (int i = 0; i < 100; i++) { _subject.AddWork(""); }

			Thread.Sleep(1500);

			_subject.Stop();

			Assert.That(_output.Count(s=>s=="Start"), Is.EqualTo(_output.Count(s=>s=="End")));
		}

		IEnumerable<string> RandomStrings(int count)
		{
			var o = new List<string>();
			var buf = new byte[10];
			for (int i = 0; i < count; i++)
			{
				_random.NextBytes(buf);
				o.Add(Encoding.ASCII.GetString(buf.Select(b => (byte)((b % 64) + 40)).ToArray()));
			}
			return o;
		}
	}
}
