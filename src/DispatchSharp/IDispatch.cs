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
		void AddConsumer(Action<T> action);
		void AddWork(T work);
		IEnumerable<Action<T>> WorkActions();
		event EventHandler<ExceptionEventArgs> Exceptions;

		void OnExceptions(Exception e);

		/// <summary> Stop consuming work and return when all in-progress work is complete </summary>
		void Stop();
	}
}