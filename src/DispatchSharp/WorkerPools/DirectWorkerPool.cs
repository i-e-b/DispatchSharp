using System;
using System.Linq;

namespace DispatchSharp
{
	/// <summary>
	/// Single threaded on-demand pool for integration testing
	/// </summary>
	public class DirectWorkerPool<T> : IWorkerPool<T>
	{
		IDispatch<T> _dispatch;
		IWorkQueue<T> _queue;

		public void SetSource(IDispatch<T> dispatch, IWorkQueue<T> queue)
		{
			_dispatch = dispatch;
			_queue = queue;
		}

		public void Start()
		{

		}

		public void Stop()
		{
		}

		public void TriggerAvailable()
		{
			IWorkQueueItem<T> work;
			while ((work = _queue.TryDequeue()).HasItem)
			{
				foreach (var action in _dispatch.WorkActions().ToArray())
				{
					try
					{
						action(work.Item);
						work.Finish();
					}
					catch (Exception ex)
					{
						work.Cancel();
						_dispatch.OnExceptions(ex);
					}
				}
			}
		}
	}
}