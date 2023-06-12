using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DispatchSharp.QueueTypes;

/// <summary>
/// A wrapper around a polling method to produce a work queue.
/// This auto-rate limits the queue using thread sleeps.
/// <p></p>
/// This queue can be fed with extra data, which will be consumed before requesting
/// more from the polling source.
/// <p></p>
/// This queue will only sleep if the polling source is empty (returns no object).
/// Sleep duration is reset whenever an item is available.
/// </summary>
public class PollingWorkQueue<T>: ISleeper, ICanStop, IWorkQueue<T>
{
	private IBackOffWaiter _sleeper;
	private readonly IPollSource<T> _pollingSource;
	private readonly object _lockObject;
	private readonly Queue<Named<T>> _queue;
	private int _sleep;
	private volatile bool _shouldStop;

	/// <summary>
	/// Create a new polling work queue with a given item source
	/// </summary>
	public PollingWorkQueue(IPollSource<T> pollingSource)
	{
		_shouldStop = false;
		_pollingSource = pollingSource;
		_lockObject = new object();
		_queue = new Queue<Named<T>>();
		_sleep = 0;
		_sleeper = new DefaultSleeper();
	}

	/// <summary>
	/// Enqueue extra work to the queue.
	/// local work will be used before the polling queue is used.
	/// </summary>
	public void Enqueue(T work, string? name = null)
	{
		lock(_lockObject)
		{
			_queue.Enqueue(new Named<T> { Value = work, Name = name });
		}
	}

	/// <summary> Try and get an item from this queue. Success is encoded in the WQI result 'HasItem' </summary>
	public IWorkQueueItem<T> TryDequeue()
	{
		lock(_lockObject)
		{
			if (_queue.Count > 0)
			{
				var data = _queue.Dequeue();
				return new WorkQueueItem<T>(data.Value, finish: _ => { }, cancel: i=>Enqueue(i, data.Name), data.Name);
			}
		}

		if (_shouldStop) return new WorkQueueItem<T>(); // don't poll if we're trying to shut-down

		if (!_pollingSource.TryGet(out var item))
		{
			SleepMore();
			return new WorkQueueItem<T>();
		}

		ResetSleep();
		return new WorkQueueItem<T>(item!, finish: _ => { }, cancel: i => Enqueue(i), null);
	}

	private void ResetSleep()
	{
		_sleep = 0;
	}

	private void SleepMore()
	{
		Interlocked.Increment(ref _sleep);
		_sleeper.Wait(_sleep);
	}

	/// <summary>
	/// Increments sleep duration and returns new sleep duration
	/// </summary>
	public int BurstSleep()
	{
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
		
	/// <inheritdoc />
	public IEnumerable<string> AllItemNames()
	{
		lock (_lockObject)
		{
			return _queue.Where(item => item.Name is not null).Select(item => item.Name ?? "");
		}
	}

	/// <inheritdoc />
	public void SetSleeper(IBackOffWaiter sleeper)
	{
		_sleeper = sleeper;
	}

	/// <summary>
	/// Immediately returns Unknown
	/// </summary>
	public QueueState BlockUntilReady()
	{
		return QueueState.Unknown;
	}

	/// <summary>
	/// The queue should stop accepting new work, including polling
	/// </summary>
	public void StopAcceptingWork()
	{
		_shouldStop = true;
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