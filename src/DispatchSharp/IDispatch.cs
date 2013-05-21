using System;
using System.Collections.Generic;

namespace DispatchSharp
{
	public class ExceptionEventArgs : EventArgs
	{
		public Exception SourceException { get; set; }
	}

	public interface IDispatch<T>
	{
		/// <summary> Maximum number of work items being processed at any one time </summary>
		int MaximumInflight { get; set; }

		/// <summary> Snapshot of number of work items being processed </summary>
		int CurrentInflight();

		/// <summary> Add an action to take when work is processed </summary>
		void AddConsumer(Action<T> action);

		/// <summary> Add a work item to process </summary>
		void AddWork(T work);

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
	}
}