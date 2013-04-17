using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace DispatchSharp.Unit.Tests
{
	[TestFixture]
	public class sketch
	{
		IDispatch<string> _subject;
		List<string> _output;

		[SetUp]
		public void setup()
		{
			_output = new List<string>();
			_subject = new Dispatch<string>(new Provider<string>(), new WorkerPool<string>(2));
		}

		[Test]
		public void quick_sketch_of_consuming_pool()
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

			_subject.Exceptions += (e,ex) => _output.Add(ex.SourceException.Message);

			_subject.AddWork("Hello");
			_subject.AddWork("THROW");
			_subject.AddWork("World");
			Thread.Sleep(1000);
			Assert.That(_output, Is.EquivalentTo(new[] { "Hello", "Woggle", "World" }));
		}
	}
}
