using System;
using System.Diagnostics.CodeAnalysis;

namespace DispatchSharp.Internal
{
	/// <summary>
	/// Contract for a wait signal
	/// </summary>
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public interface IWaitHandle
	{
		/// <summary>
		/// Wait for signal to be Set
		/// </summary>
		bool WaitOne();
		
		/// <summary>
		/// Wait for signal to be Set,
		/// wait up to timeout, but no longer.
		/// </summary>
		bool WaitOne(TimeSpan timeout);

		/// <summary>
		/// Set signal, unblocking all waiting threads
		/// </summary>
		void Set();

		/// <summary>
		/// Reset thread, causing any waiting threads to block
		/// </summary>
		void Reset();

		/// <summary>
		/// Returns true if WaitOne will return immediately.
		/// Returns false if WaitOne would block.
		/// </summary>
		bool IsSet();
	}
}