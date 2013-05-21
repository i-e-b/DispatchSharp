using System;

namespace DispatchSharp.Internal
{
	public class Default
	{
		public static int ThreadCount
		{
			get
			{
				return Environment.ProcessorCount;
			}
		}
	}
}