using System;
using System.Collections.Generic;
using DispatchSharp.QueueTypes;
using DispatchSharp.WorkerPools;

namespace DispatchSharp
{
	public partial class Dispatch<T>
	{
		/// <summary>
		/// Create a dispatcher with defaults for processing non-persistent items
		/// using all the CPU cores on the local machine
		/// </summary>
		/// <param name="name">Name of the dispatcher (useful for debugging)</param>
		public static IDispatch<T> CreateDefaultMultithreaded(string name)
		{
			return new Dispatch<T>(new InMemoryWorkQueue<T>(), new ThreadedWorkerPool<T>(name));
		}

		/// <summary>
		/// Process a batch of work with all defaults and a given action.
		/// Blocks until all work complete.
		/// </summary>
		public static void ProcessBatch(string name, IEnumerable<T> batch, Action<T> task, Action<Exception> exceptionHandler = null)
		{
			var dispatcher = CreateDefaultMultithreaded(name);
			if (exceptionHandler != null) dispatcher.Exceptions += (s,e) => exceptionHandler(e.SourceException); 
			dispatcher.AddConsumer(task);
			dispatcher.Start();
			dispatcher.AddWork(batch);
			dispatcher.WaitForEmptyQueueAndStop();
		}
	}
}