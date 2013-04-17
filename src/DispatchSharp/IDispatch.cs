using System;
using System.Collections.Generic;
using System.Threading;

namespace DispatchSharp.Unit.Tests
{
	public class ExceptionEventArgs : EventArgs
	{
		public Exception SourceException { get; set; }
	}

	public interface IDispatch<T>
	{
		void AddConsumer(Action<T> action);
		void AddWork(T work);
		WaitHandle Available { get; }
		IEnumerable<Action<T>> WorkActions();
		event EventHandler<ExceptionEventArgs> Exceptions;

		void OnExceptions(Exception e);
	}
}