using System;
using System.Collections.Generic;

namespace DispatchSharp.QueueTypes
{
	public class InMemoryWorkQueue<T> : IWorkQueue<T>
	{
		readonly object _lockObject;
		readonly Queue<T> _queue;

		public InMemoryWorkQueue()
		{
			_queue = new Queue<T>();
			_lockObject = new object();
		}

		public void Enqueue(T work)
		{
			lock (_lockObject)
			{
				_queue.Enqueue(work);
			}
		}

		public IWorkQueueItem<T> TryDequeue()
		{
			lock(_lockObject)
			{
				if (_queue.Count < 1) return NoItem();

				return new WorkQueueItem<T>(_queue.Dequeue(), null, Enqueue);
			}
		}

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
	}

	public class WorkQueueItem<T>:IWorkQueueItem<T>
	{
		readonly Action<T> _finish;
		readonly Action<T> _cancel;
		public bool HasItem { get; set; }
		public T Item { get; set; }

		public WorkQueueItem()
		{
			HasItem = false;
		}
		public WorkQueueItem(T item, Action<T> finish, Action<T> cancel)
		{
			_finish = finish ?? (t => { });
			_cancel = cancel ?? (t => { });
			HasItem = true;
			Item = item;
		}

		public void Finish()
		{
			_finish(Item);
		}

		public void Cancel()
		{
			_cancel(Item);
		}
	}
}