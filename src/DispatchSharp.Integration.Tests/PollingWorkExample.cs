﻿using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
// ReSharper disable AssignNullToNotNullAttribute

namespace DispatchSharp.Integration.Tests
{
	[TestFixture]
	public class PollingWorkExample
	{
		[Test]
		public void using_a_polling_source_to_do_long_term_work()
		{
			var result = new List<string>();
			
			var source = new SampleSource(10);
			var dispatcher = Dispatch<string>.PollAndProcess("test", source, threadCount: 1);

			dispatcher.AddConsumer(result.Add);

			dispatcher.Start();
			Thread.Sleep(250);
			dispatcher.Stop();

			Assert.That(result, Is.EquivalentTo(
				new []{"item 0", "item 1", "item 2", "item 3", "item 4", "item 5", "item 6", "item 7", "item 8", "item 9"}
				));
		}

		public class SampleSource:IPollSource<string>
		{
			readonly Queue<string> _items;

			public SampleSource(int i)
			{
				_items = new Queue<string>(i);
				for (int j = 0; j < i; j++)
				{
					_items.Enqueue("item "+j);
				}
			}

			public bool TryGet(out string item)
			{
				if (_items.Count <= 0)
				{
					item = null;
					return false;
				}

				item = _items.Dequeue();
				return true;
			}
		}
	}
}