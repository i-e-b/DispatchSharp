using System.Collections.Generic;

namespace DispatchSharp.Unit.Tests
{
	public class Provider<T> : IWorkProvider<T>
	{
		readonly object _lockObject;
		readonly Queue<T> _queue;

		public Provider()
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

		public bool TryDequeue(out T item)
		{
			lock(_lockObject)
			{
				item = default(T);
				if (_queue.Count < 1) return false;

				item = _queue.Dequeue();
				return true;
			}
		}
	}
}