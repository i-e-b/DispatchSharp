namespace DispatchSharp.Unit.Tests
{
	public interface IWorkProvider<T>
	{
		void Enqueue(T work);
		bool TryDequeue(out T item);
	}
}