DispatchSharp
=============

An experimental library to make multi-threaded dispatch code more testable.

Models a job dispatch pattern and provides both threaded and non threaded implementations.

Getting Started
---------------
```csharp
IDispatcher dispatcher = new Dispatch<object>(
		new InMemoryWorkQueue<object>(),
		new ThreadedWorkerPool<object>("myDispatcher", Environment.ProcessorCount)
	);

dispatcher.AddConsumer(MyWorkMethod);

for (int i = 0; i < 10; i++)
{
	dispatcher.AddWork(new object());
}
```

with a method defined like
```csharp
void MyWorkMethod(object obj)
{
	. . .
}
```

Todo:
------

Work queue:
 * self-populating queues
 * persistent store and forward for waiting jobs.
 