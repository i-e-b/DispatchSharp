using System;
using System.Collections.Generic;
using DispatchSharp.Internal;

namespace DispatchSharp
{
	/// <summary>
	/// Arguments for events triggered by uncaught exceptions
	/// </summary>
	public class ExceptionEventArgs<T> : EventArgs
	{

		/// <summary>
		/// Triggering exception
		/// </summary>
		public Exception SourceException { get; set; } = UninitialisedValues.NoException;

		/// <summary>
		/// The work item that caused the exception.
		/// You can use the Cancel or Finish methods to control work
		/// rescheduling.
		/// If you don't call Cancel, the work item will be Finished by default.
		/// </summary>
		public IWorkQueueItem<T> WorkItem { get; set; } = new InvalidQueueItem<T>();
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
		
		/// <summary> Snapshot of number of work items in the queue (both being processed and waiting) </summary>
		int CurrentQueued();

		/// <summary>
		/// Add an action to take when work is processed.
		/// Simple actions have their work removed from the queue by default
		/// after completion or failure. If you wish to cancel a failed work item,
		/// use the Exceptions event
		/// </summary>
		void AddConsumer(Action<T> action);

		/// <summary> Add a work item to process </summary>
		void AddWork(T work);

		/// <summary> Add multiple work items to process </summary>
		void AddWork(IEnumerable<T> workList);

		/// <summary> Add a named work item to process </summary>
		void AddWork(T work, string? name);

		/// <summary> Add multiple work items to process, each with the same name </summary>
		void AddWork(IEnumerable<T> workList, string? name);

		/// <summary> All consumers added to this dispatcher </summary>
		IEnumerable<Action<T>> AllConsumers();

		/// <summary> Event triggered when a consumer throws an exception </summary>
		event EventHandler<ExceptionEventArgs<T>> Exceptions;

		/// <summary> Start consuming work and continue until stopped </summary>
		void Start();

		/// <summary> Stop consuming work and return when all in-progress work is complete </summary>
		void Stop();

		/// <summary>
		/// Continue consuming work and return when the queue reports 0 items waiting. 
		/// </summary>
		void WaitForEmptyQueueAndStop();

		/// <summary>
		/// Continue consuming work and return when the queue reports 0 items waiting. 
		/// </summary>
		/// <param name="maxWait">Maximum duration to wait. The dispatcher will be stopped if this duration is exceeded</param>
		void WaitForEmptyQueueAndStop(TimeSpan maxWait);
	}

	/// <summary>
	/// Interfaces for dispatcher internal methods.
	/// These should not be used in production code.
	/// </summary>
	public interface IDispatchInternal<T> {
		/// <summary> INTERNAL: Trigger to call when a consumer throws an exception </summary>
		void OnExceptions(Exception e, IWorkQueueItem<T> work);
	}
}