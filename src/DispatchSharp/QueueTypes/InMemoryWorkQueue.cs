using System;
using System.Collections.Generic;
using DispatchSharp.Internal;

namespace DispatchSharp.QueueTypes
{
	public class InMemoryWorkQueue<T> : IWorkQueue<T>
	{
		readonly object _lockObject;
		readonly Queue<T> _queue;
		readonly IWaitHandle _waitHandle;

		public InMemoryWorkQueue()
		{
			_queue = new Queue<T>();
			_lockObject = new object();
			_waitHandle = new CrossThreadWait(false);
		}

		public void Enqueue(T work)
		{
			lock (_lockObject)
			{
				_queue.Enqueue(work);
				_waitHandle.Set();
			}
		}

		public IWorkQueueItem<T> TryDequeue()
		{
			lock(_lockObject)
			{
				if (_queue.Count < 1) return NoItem();
				var data = _queue.Dequeue();
				
				if (_queue.Count < 1) _waitHandle.Reset();
				return new WorkQueueItem<T>(data, null, Enqueue);
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

		public void BlockUntilReady() {
			_waitHandle.WaitOne();
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