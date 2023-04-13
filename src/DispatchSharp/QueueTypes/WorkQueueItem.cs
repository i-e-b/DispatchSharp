using System;
using System.Threading;

namespace DispatchSharp.QueueTypes
{
	/// <summary>
	/// A default work queue item wrapper
	/// </summary>
	/// <typeparam name="T">Type of the underlying queue item</typeparam>
	public class WorkQueueItem<T>:IWorkQueueItem<T>
	{
		readonly Action<T> _finish;
		readonly Action<T> _cancel;
		object? _completionActionToken;

		/// <summary>
		/// Has an item been dequeued?
		/// If false, Item will be default value (i.e. null)
		/// </summary>
		public bool HasItem { get; set; }

		/// <summary>
		/// Queue item if one was available
		/// </summary>
		public T Item { get; set; }

		/// <summary>
		/// Given name of item if item was available and a name was supplied
		/// </summary>
		public string? Name { get; set; }

		/// <summary>
		/// Create an empty item (represents an unsuccessful dequeue)
		/// </summary>
		public WorkQueueItem()
		{
			Item = default!;
			HasItem = false;
			_completionActionToken = null;
			
			_finish = _ => { };
			_cancel = _ => { };
		}

		/// <summary>
		/// Create a populated item with optional finish and cancel items
		/// </summary>
		/// <param name="item">Item dequeued</param>
		/// <param name="finish">Finish action (may be null)</param>
		/// <param name="cancel">Cancel action (may be null)</param>
		/// <param name="name">Optional: name of queue item</param>
		public WorkQueueItem(T item, Action<T>? finish, Action<T>? cancel, string? name)
		{
			_finish = finish ?? (_ => { });
			_cancel = cancel ?? (_ => { });
			HasItem = true;
			Item = item;
			Name = name;
			_completionActionToken = new object();
		}

		/// <summary>
		/// Call this to permanently remove an item from the queue.
		/// This wrapper has a safety to ensure that only one of Finish/Cancel can be called, 
		/// and that the underlying method is called only once.
		/// </summary>
		public void Finish()
		{
			var token = Interlocked.Exchange<object?>(ref _completionActionToken, null);
			if (token == null) return;
			_finish(Item);
		}

		/// <summary>
		/// Call this to cancel the dequeue and return item to work queue.
		/// There is no guarantee where the item will be returned (head, end or somewhere in the middle)
		/// </summary>
		public void Cancel()
		{
			var token = Interlocked.Exchange<object?>(ref _completionActionToken, null);
			if (token == null) return;
			_cancel(Item);
		}
	}
}