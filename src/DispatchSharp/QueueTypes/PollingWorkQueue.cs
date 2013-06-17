using System.Collections.Generic;
using System.Threading;

namespace DispatchSharp.QueueTypes
{
	/// <summary>
	/// A wrapper around a polling method to produce a work queue.
	/// This auto-rate limits the queue using thread sleeps.
	/// </summary>
	public class PollingWorkQueue<T>: ISleeper, IWorkQueue<T>
	{
		readonly IPollSource<T> _pollingSource;
		readonly object _lockObject;
		readonly Queue<T> _queue;
		int _sleep;

		/// <summary>
		/// Create a new polling work queue with a given item source
		/// </summary>
		public PollingWorkQueue(IPollSource<T> pollingSource)
		{
			_pollingSource = pollingSource;
			_lockObject = new object();
			_queue = new Queue<T>();
			_sleep = 0;
		}

		/// <summary>
		/// Enqueue extra work to the queue.
		/// local work will be used before the polling queue is used.
		/// </summary>
		public void Enqueue(T work)
		{
			lock(_lockObject)
			{
				_queue.Enqueue(work);
			}
		}

		/// <summary> Try and get an item from this queue. Success is encoded in the WQI result 'HasItem' </summary>
		public IWorkQueueItem<T> TryDequeue()
		{
			T item;
			lock(_lockObject)
			{
				if (_queue.Count > 0)
				{
					var data = _queue.Dequeue();
					return new WorkQueueItem<T>(data, finish: m => { }, cancel: Enqueue);
				}
			}


			if (!_pollingSource.TryGet(out item))
			{
				SleepMore();
				return new WorkQueueItem<T>();
			}

			ResetSleep();
			return new WorkQueueItem<T>(item, finish: m => { }, cancel: Enqueue);
		}

		void ResetSleep()
		{
			_sleep = 0;
		}

		void SleepMore()
		{
			Thread.Sleep(BurstSleep());
		}

		/// <summary>
		/// Increments sleep duration and returns new sleep duration
		/// </summary>
		public int BurstSleep()
		{
			_sleep = (_sleep < 255) ? (_sleep * 2) + 1 : 255;
			return _sleep;
		}

		/// <summary> Returns length of extra queue items </summary>
		public int Length()
		{
			lock(_lockObject)
			{
				return _queue.Count;
			}
		}

		/// <summary>
		/// Immediately returns true
		/// </summary>
		public bool BlockUntilReady()
		{
			return true;
		}
	}

	/// <summary>
	/// Interface for reading sleeper progress
	/// </summary>
	public interface ISleeper
	{
		/// <summary>
		/// Increments sleep duration and returns new sleep duration
		/// </summary>
		int BurstSleep();
	}
}