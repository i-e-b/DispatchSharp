namespace DispatchSharp.Unit.Tests
{
	public interface IWorkerPool<T>
	{
		void SetSource(IDispatch<T> dispatch, IWorkProvider<T> provider);
		void Start();
	}
}