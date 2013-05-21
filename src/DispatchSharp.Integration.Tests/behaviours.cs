using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace DispatchSharp.Integration.Tests
{
	[TestFixture]
	public abstract class behaviours
	{
		public IDispatch<string> _subject;
		public List<string> _output;
		readonly Random _random = new Random();

		[SetUp]
		public abstract void setup();

		[Test, Explicit("slow test")]
		public void fuzzing_test ()
		{
			var input = RandomStrings(1000);
			_subject.AddConsumer(s => {
				Thread.Sleep(_random.Next(0,10));
				_output.Add(s);
				Thread.Sleep(_random.Next(0,10));
			});
			_subject.Start();
			foreach (var str in input) { 
				Thread.Sleep(_random.Next(0,10));
				_subject.AddWork(str);
			}

			Thread.Sleep(2000);
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
			_subject.Start();

			_subject.AddWork("Hello");
			_subject.AddWork("World");

			Thread.Sleep(1000);
			Assert.That(_output, Is.EquivalentTo(new[] { "Hello", "World" }));
		}

		[Test]
		public void a_consumer_that_throws_an_exception_fires_an_event_but_does_not_stop_the_worker ()
		{
			var once = true;
			_subject.AddConsumer(s =>
			{
				if (s == "THROW") {
					if (once) {
						once = false;
						throw new Exception("Woggle");
					} else return;
				}
				Console.WriteLine(s);
				_output.Add(s);
			});
			_subject.Start();

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
