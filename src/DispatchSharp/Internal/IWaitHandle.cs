namespace DispatchSharp.Internal
{
	public interface IWaitHandle
	{
		bool WaitOne();
		void Set();
		void Reset();

		bool IsSet();
	}
}