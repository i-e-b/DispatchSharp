using System;
using System.Collections.Generic;
using System.Linq;
using DispatchSharp.Internal;

namespace DispatchSharp.QueueTypes
{
	/// <summary>
	/// A non-persistent lock-based worker queue.
	/// </summary>
	/// <typeparam name="T">Type of items to be stored</typeparam>
	public class InMemoryWorkQueue<T> : IWorkQueue<T>
	{
		readonly object _lockObject;
		readonly Queue<Named<T>> _queue;
		readonly IWaitHandle _waitHandle;

		/// <summary>
		/// Create a new empty worker queue
		/// </summary>
		public InMemoryWorkQueue()
		{
			_queue = new Queue<Named<T>>();
			_lockObject = new object();
			_waitHandle = new CrossThreadWait(false);
		}

		/// <summary> Add an item to the queue </summary>
		public void Enqueue(T work, string? name = null)
		{
			lock (_lockObject)
			{
				_queue.Enqueue(new Named<T> { Value = work, Name = name });
				_waitHandle.Set();
			}
		}

		/// <summary> Try and get an item from this queue. Success is encoded in the IWorkQueueItem result 'HasItem' </summary>
		public IWorkQueueItem<T> TryDequeue()
		{
			lock(_lockObject)
			{
				if (_queue.Count < 1) return NoItem();
				var data = _queue.Dequeue();
				
				if (_queue.Count < 1) _waitHandle.Reset();
				return new WorkQueueItem<T>(data.Value, null, item => Enqueue(item, data.Name), data.Name);
			}
		}

		/// <summary> Current queue length </summary>
		public int Length()
		{
			lock (_lockObject)
			{
				return _queue.Count;
			}
		}

		IWorkQueueItem<T> NoItem()
		{
			return new WorkQueueItem<T>();
		}

		/// <summary>
		/// Blocks the current thread until an item is available on the queue.
		/// Will block for up to 100ms. Does not guarantee an item will be dequeued successfully.
		/// </summary>
		public bool BlockUntilReady() {
			return _waitHandle.WaitOne(TimeSpan.FromMilliseconds(100));
		}

		/// <inheritdoc />
		public IEnumerable<string> AllItemNames()
		{
			lock (_lockObject)
			{
				return _queue.Where(item => item.Name is not null).Select(item => item.Name ?? "");
			}
		}

		/// <summary>
		/// Ignored
		/// </summary>
		public void SetSleeper(IBackOffWaiter sleeper) { }
	}
}