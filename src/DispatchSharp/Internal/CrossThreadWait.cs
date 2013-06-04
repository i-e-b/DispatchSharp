using System;
using System.Threading;

namespace DispatchSharp.Internal
{
	class CrossThreadWait : IWaitHandle
	{
		readonly AutoResetEvent _base;

		public CrossThreadWait(bool initialSetting)
		{
			_base = new AutoResetEvent(initialSetting);
		}
		public bool WaitOne()
		{
			return _base.WaitOne();
		}

		public bool WaitOne(TimeSpan timeout)
		{
			return _base.WaitOne(timeout);
		}

		public void Set()
		{
			_base.Set();
		}

		public void Reset()
		{
			_base.Reset();
		}

		public bool IsSet()
		{
			return _base.WaitOne(0);
		}
	}
}