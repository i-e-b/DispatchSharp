DispatchSharp
=============

https://www.nuget.org/packages/DispatchSharp/

A library to make multi-threaded dispatch code more testable.

Models a job dispatch pattern and provides both threaded and non threaded implementations.

Getting Started
---------------

Doing a batch of work:
```csharp
void DoBatch(IEnumerable<object> workToDo) {
	var dispatcher = Dispatch<object>.CreateDefaultMultiThreaded("MyTask");

	dispatcher.AddConsumer(MyWorkMethod);
	dispatcher.Start();
	dispatcher.AddWork(workToDo);
	dispatcher.WaitForEmptyQueueAndStop();	// only call this if you're not filling the queue from elsewhere
}
```

or

```csharp
Dispatch<int>.ProcessBatch("BatchRequests", Enumerable.Range(0, times).ToArray(), i => {
        . . .
    },
    threadCount,
    ex => {
        Console.WriteLine("Error: " + ex.Message);
    }
);
```

Handling long running incoming jobs:

```csharp
dispatcher = Dispatch<object>.CreateDefaultMultiThreaded("MyService");
dispatcher.AddConsumer(MyWorkMethod);
dispatcher.Start();
.
.
.
dispatcher.AddWork(...);
```

Using a polling method to handle incoming jobs in a long-running process:

```csharp
var dispatcher = Dispatch<object>.PollAndProcess("MyService", myPollingSource);
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

Multiple consumers
------------------

A dispatcher can be given multiple consumers.
In this case, each consumer will get given each work item added.

So with

```csharp
dispatcher = Dispatch<string>.CreateDefaultMultiThreaded("MyService");
dispatcher.AddConsumer(wi=> Console.Write($" Consumer 1, work item {wi};"));
dispatcher.AddConsumer(wi=> Console.Write($" Consumer 2, work item {wi};"));
dispatcher.AddConsumer(wi=> Console.Write($" Consumer 3, work item {wi};"));
dispatcher.Start();

dispatcher.AddWork("1");
Console.WriteLine();
dispatcher.AddWork("2");
Console.WriteLine();
dispatcher.AddWork("3");
Console.WriteLine();
```

We should get output similar to

```
 Consumer 1, work item 1; Consumer 2, work item 1; Consumer 3, work item 1;
 Consumer 1, work item 2; Consumer 2, work item 2; Consumer 3, work item 2;
 Consumer 1, work item 3; Consumer 2, work item 3; Consumer 3, work item 3;
```

Parallelism Modes
-----------------

By default, work items are processed in parallel, with each consumer being run
in sequential order ('work parallel').

If you need to have each work item handled strictly in-order, but each consumer
can be run in parallel, use the 'task parallel' mode:

```csharp
dispatcher = Dispatch<string>.CreateTaskParallelMultiThreaded("MyService");
dispatcher.AddConsumer(wi=> Console.Write($" Consumer 1, work item {wi};"));
dispatcher.AddConsumer(wi=> Console.Write($" Consumer 2, work item {wi};"));
dispatcher.AddConsumer(wi=> Console.Write($" Consumer 3, work item {wi};"));
dispatcher.Start();
```