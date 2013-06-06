using System;
using System.Collections.Generic;

namespace DispatchSharp
{
	/// <summary>
	/// Arguments for events triggered by uncaught exceptions
	/// </summary>
	public class ExceptionEventArgs : EventArgs
	{
		/// <summary>
		/// Triggering exception
		/// </summary>
		public Exception SourceException { get; set; }
	}

	/// <summary>
	/// Dispatcher co-ordinated a worker pool with a work item queue,
	/// and keeps track of actions to be taken with work items.
	/// </summary>
	/// <typeparam name="T">Type of work items to be processed</typeparam>
	public interface IDispatch<T>
	{
		/// <summary> Maximum number of work items being processed at any one time </summary>
		int MaximumInflight();

		/// <summary>
		/// Maximum number of work items being processed at any one time
		/// </summary>
		void SetMaximumInflight(int max);

		/// <summary> Snapshot of number of work items being processed </summary>
		int CurrentInflight();

		/// <summary> Add an action to take when work is processed </summary>
		void AddConsumer(Action<T> action);

		/// <summary> Add a work item to process </summary>
		void AddWork(T work);
		
		/// <summary> Add multiple work items to process </summary>
		void AddWork(IEnumerable<T> workList);

		/// <summary> All consumers added to this dispatcher </summary>
		IEnumerable<Action<T>> AllConsumers();

		/// <summary> Event triggered when a consumer throws an exception </summary>
		event EventHandler<ExceptionEventArgs> Exceptions;

		/// <summary> Trigger to call when a consumer throws an exception </summary>
		void OnExceptions(Exception e);
		
		/// <summary> Start consuming work and continue until stopped </summary>
		void Start();

		/// <summary> Stop consuming work and return when all in-progress work is complete </summary>
		void Stop();

		/// <summary>
		/// Continue consuming work and return when the queue reports 0 items waiting. 
		/// </summary>
		void WaitForEmptyQueueAndStop();
	}
}