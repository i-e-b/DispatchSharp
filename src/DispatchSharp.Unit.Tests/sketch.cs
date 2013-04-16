using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace DispatchSharp.Unit.Tests
{
	[TestFixture]
	public class sketch
	{
		[Test]
		public void quick_sketch_of_consuming_pool()
		{
			var output = new List<string>();

			IDispatch<string> thing = new Dispatch<string>(new Provider<string>(), new WorkerPool<string>(2));

			thing.AddConsumer(s =>
			{
				Console.WriteLine(s);
				output.Add(s);
			});

			thing.AddWork("Hello");
			thing.AddWork("World");

			Thread.Sleep(1000);
			Assert.That(output, Is.EquivalentTo(new[] { "Hello", "World" }));
		}
	}

	public class Dispatch<T> : IDispatch<T>
	{
		readonly IWorkProvider<T> _provider;
		readonly IWorkerPool<T> _pool;
		readonly IList<Action<T>> _workActions;
		readonly object _lockObject;

		public WaitHandle Available { get; private set; }

		public Dispatch(IWorkProvider<T> workProvider, IWorkerPool<T> workerPool)
		{
			_provider = workProvider;
			_pool = workerPool;

			_lockObject = new object();
			_workActions = new List<Action<T>>();

			Available = new AutoResetEvent(true);
			_pool.SetSource(this, _provider);
		}

		public void AddConsumer(Action<T> action)
		{
			_pool.Start();
			lock (_lockObject)
			{
				_workActions.Add(action);
			}
		}

		public void AddWork(T work)
		{
			_provider.Enqueue(work);
			((AutoResetEvent)Available).Set();
		}

		public IEnumerable<Action<T>> WorkActions()
		{
			lock (_lockObject)
			{
				foreach (var workAction in _workActions)
				{
					yield return workAction;
				}
			}
		}

	}

	public interface IDispatch<T>
	{
		void AddConsumer(Action<T> action);
		void AddWork(T work);
		WaitHandle Available { get; }
		IEnumerable<Action<T>> WorkActions();
	}

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

	public interface IWorkProvider<T>
	{
		void Enqueue(T work);
		bool TryDequeue(out T item);
	}

	public class WorkerPool<T> : IWorkerPool<T>
	{
		readonly Thread[] _pool;
		IDispatch<T> _dispatch;
		volatile object _started;
		IWorkProvider<T> _provider;

		public WorkerPool(int threadCount)
		{
			_pool = new Thread[threadCount];
			_started = null;
		}

		public void SetSource(IDispatch<T> dispatch, IWorkProvider<T> provider)
		{
			_dispatch = dispatch;
			_provider = provider;
		}

		public void Start()
		{
			// ReSharper disable CSharpWarnings::CS0420
			var closedObject = new object();
			if (Interlocked.CompareExchange(ref _started, closedObject, null) != null) return;
			// ReSharper restore CSharpWarnings::CS0420

			for (int i = 0; i < _pool.Length; i++)
			{
				_pool[i] = new Thread(() => WorkLoop(closedObject))
				{
					IsBackground = true,
					Name = "Pool_" + i
				};
				_pool[i].Start();
			}
		}

		void WorkLoop(object reference)
		{
			while (_started == reference)
			{
				_dispatch.Available.WaitOne();
				T work;
				while (_provider.TryDequeue(out work))
				{
					foreach (var action in _dispatch.WorkActions().ToArray())
					{
						try
						{
							action(work);
						}
						catch (Exception ex)
						{
							// todo: have an event and trigger it on exception
						}
					}
				}
			}
		}
	}

	public interface IWorkerPool<T>
	{
		void SetSource(IDispatch<T> dispatch, IWorkProvider<T> provider);
		void Start();
	}
}
