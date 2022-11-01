﻿using System.Collections.Generic;
using System.Threading;
using DispatchSharp.QueueTypes;
using DispatchSharp.WorkerPools;
using NUnit.Framework;
using System.Linq;
// ReSharper disable PossibleNullReferenceException
// ReSharper disable AssignNullToNotNullAttribute

namespace DispatchSharp.Integration.Tests
{
	[TestFixture]
	public class Direct_InMemory_tests : behaviours
	{
		[SetUp]
		public override void setup()
		{
			_output = new List<string>();
			_subject = new Dispatch<string>(
				new InMemoryWorkQueue<string>(),
				new DirectWorkerPool<string>());
		}

		[Test]
		public void executes_all_actions_before_returning ()
		{
			_subject.AddConsumer(s =>
			{
				_output.Add("Start");
				_output.Add("End");
			});

			_subject.Start();
			for (int i = 0; i < 10; i++) { _subject.AddWork(""); }
			Thread.Sleep(10);
			_subject.Stop();

			Assert.That(_output.Count, Is.EqualTo(20));
			Assert.That(_output.Count(s=>s=="Start"), Is.EqualTo(_output.Count(s=>s=="End")));
		}
	}
}
