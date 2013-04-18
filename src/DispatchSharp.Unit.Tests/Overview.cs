using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace DispatchSharp.Unit.Tests
{
	[TestFixture]
	public class Overview
	{
		IDispatch<string> _subject;
		List<string> _output;

		[SetUp]
		public void setup()
		{
			_output = new List<string>();
			_subject = new Dispatch<string>(new InMemoryWorkQueue<string>(), new WorkerPool<string>("Test", 2));
		}

		[Test, Explicit]
		public void fuzzing_test ()
		{
			var input = RandomStrings(1000);
			_subject.AddConsumer(s => _output.Add(s));
			foreach (var str in input) { 
				Thread.Sleep(1);
				_subject.AddWork(str);
			}

			Thread.Sleep(1000);
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

		IEnumerable<string> RandomStrings(int count)
		{
			var o = new List<string>();
			var r = new Random();
			var buf = new byte[10];
			for (int i = 0; i < count; i++)
			{
				r.NextBytes(buf);
				o.Add(Encoding.ASCII.GetString(buf.Select(b => (byte)((b % 64) + 40)).ToArray()));
			}
			return o;
		}
	}
}
