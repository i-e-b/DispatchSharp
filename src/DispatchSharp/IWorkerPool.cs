namespace DispatchSharp.Unit.Tests
{
	public interface IWorkerPool<T>
	{
		void SetSource(IDispatch<T> dispatch, IWorkQueue<T> queue);
		void Start();
	}
}