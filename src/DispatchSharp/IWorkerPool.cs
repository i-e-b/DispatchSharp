namespace DispatchSharp
{
	public interface IWorkerPool<T>
	{
		void SetSource(IDispatch<T> dispatch, IWorkQueue<T> queue);
		void Start();
		void Stop();
		IWaitHandle Available { get; }
	}
}