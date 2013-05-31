using System;

namespace DispatchSharp.Internal
{
	/// <summary>
	/// Defaults used internally
	/// </summary>
	public class Default
	{
		/// <summary>
		/// Default number of threads to pool on this machine.
		/// </summary>
		public static int ThreadCount
		{
			get
			{
				return Environment.ProcessorCount;
			}
		}
	}
}