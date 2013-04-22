namespace DispatchSharp
{
	/// <summary>
	/// Single threaded on-demand pool for integration testing
	/// </summary>
	public class DirectWorkerPool<T> : IWorkerPool<T>
	{
		public void SetSource(IDispatch<T> dispatch, IWorkQueue<T> queue)
		{
		}

		public void Start()
		{
		}

		public void Stop()
		{
		}

		public IWaitHandle Available { get; private set; }
	}
}