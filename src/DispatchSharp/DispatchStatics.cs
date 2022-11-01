using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DispatchSharp.Internal;
using DispatchSharp.QueueTypes;
using DispatchSharp.WorkerPools;

namespace DispatchSharp
{
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public partial class Dispatch<T>
	{
		/// <summary>
		/// Create a dispatcher with defaults for processing non-persistent items
		/// using all the CPU cores on the local machine
		/// </summary>
		/// <param name="name">Name of the dispatcher (useful for debugging)</param>
		/// <param name="threadCount">Number of thread to use. Default is number of cores in the host.</param>
		public static IDispatch<T> CreateDefaultMultiThreaded(string name, int threadCount = 0)
		{
			var threads = (threadCount > 0) ? threadCount : Default.ThreadCount;
			return new Dispatch<T>(new InMemoryWorkQueue<T>(), new ThreadedWorkerPool<T>(name))
				{InflightLimit = threads};
		}

		/// <summary>
		/// Poll a source object for data and act on it.
		/// You must add a consumer to the returned dispatcher, then start
		/// the dispatcher.
		/// </summary>
		/// <param name="name">Name of the dispatcher (useful for debugging)</param>
		/// <param name="source">object to poll for data</param>
		/// <param name="threadCount">Number of thread to use. Default is number of cores in the host.</param>
		public static IDispatch<T> PollAndProcess(string name, IPollSource<T> source, int threadCount = 0)
		{
			var threads = (threadCount > 0) ? threadCount : Default.ThreadCount;
			return new Dispatch<T>(new PollingWorkQueue<T>(source), new ThreadedWorkerPool<T>(name))
				{InflightLimit = threads};
		}

		/// <summary>
		/// Process a batch of work with all defaults and a given action.
		/// Blocks until all work complete.
		/// </summary>
		public static void ProcessBatch(
			string name,
			IEnumerable<T> batch,
			Action<T> task,
			int threadCount = 0,
			Action<Exception>? exceptionHandler = null)
		{
			var dispatcher = CreateDefaultMultiThreaded(name, threadCount);
			if (exceptionHandler != null) dispatcher.Exceptions += (_,e) => exceptionHandler(e.SourceException); 
			dispatcher.AddConsumer(task);
			dispatcher.AddWork(batch);
			dispatcher.Start();
			dispatcher.WaitForEmptyQueueAndStop();
		}
	}
}