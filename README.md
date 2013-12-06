DispatchSharp
=============

A library to make multi-threaded dispatch code more testable.

Models a job dispatch pattern and provides both threaded and non threaded implementations.

Getting Started
---------------
Doing a batch of work:
```csharp
void DoBatch(IEnumerable<object> workToDo) {
	var dispatcher = Dispatch<object>.CreateDefaultMultithreaded("MyTask");

	dispatcher.AddConsumer(MyWorkMethod);
	dispatcher.Start();
	dispatcher.AddWork(workToDo);
	dispatcher.WaitForEmptyQueueAndStop();	// only call this if you're not filling the queue from elsewhere
}
```

Handling long running incoming jobs:
```csharp
dispatcher = Dispatch<object>.CreateDefaultMultithreaded("MyService");
dispatcher.AddConsumer(MyWorkMethod);
dispatcher.Start();
.
.
.
dispatcher.AddWork(...);
```

Using a polling method to handle incoming jobs in a long-running process:
```csharp
var dispatcher = Dispatch<object>.PollAndProces("MyService", myPollingSource);
dispatcher.AddConsumer(MyWorkMethod);
dispatcher.Start();
```


with a method defined like
```csharp
void MyWorkMethod(object obj)
{
	. . .
}
```
