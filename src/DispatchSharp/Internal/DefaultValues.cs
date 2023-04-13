using System;
using System.Collections.Generic;

namespace DispatchSharp.Internal;

/// <summary>
/// Default values for null suppression
/// </summary>
internal static class UninitialisedValues
{
    public static readonly Exception NoException = new();
    public static IDispatch<T> InvalidDispatch<T>() => new InvalidDispatch<T>();
    public static IWorkQueue<T> InvalidQueue<T>() => new InvalidQueue<T>();
}

/// <summary>
/// Represents a work queue that has not been correctly configured
/// </summary>
internal class InvalidQueue<T>:IWorkQueue<T>
{
    private const string InvalidQueueMessage = "Work queue has not been configured.";
    public void Enqueue(T work, string? name = null) => throw new Exception(InvalidQueueMessage);
    public IWorkQueueItem<T> TryDequeue() => throw new Exception(InvalidQueueMessage);
    public int Length() => throw new Exception(InvalidQueueMessage);
    public bool BlockUntilReady() => throw new Exception(InvalidQueueMessage);
    public IEnumerable<string> AllItemNames() => throw new Exception(InvalidQueueMessage);
}

/// <summary>
/// Represents a dispatch agent that has not been correctly configured
/// </summary>
internal class InvalidDispatch<T>:IDispatch<T>
{
    private const string InvalidDispatchMessage = "Dispatch has not been configured.";
    
    #pragma warning disable CS0067 // Event never used
    public event EventHandler<ExceptionEventArgs<T>>? Exceptions;
    #pragma warning restore CS0067
    
    public int MaximumInflight() => throw new Exception(InvalidDispatchMessage);
    public void SetMaximumInflight(int max) => throw new Exception(InvalidDispatchMessage);
    public int CurrentInflight() => throw new Exception(InvalidDispatchMessage);
    public int CurrentQueued() => throw new Exception(InvalidDispatchMessage);
    public void AddConsumer(Action<T> action) => throw new Exception(InvalidDispatchMessage);
    public void AddWork(T work) => throw new Exception(InvalidDispatchMessage);
    public void AddWork(IEnumerable<T> workList) => throw new Exception(InvalidDispatchMessage);
    public void AddWork(T work, string? name) => throw new Exception(InvalidDispatchMessage);
    public void AddWork(IEnumerable<T> workList, string? name) => throw new Exception(InvalidDispatchMessage);
    public IEnumerable<Action<T>> AllConsumers() => throw new Exception(InvalidDispatchMessage);
    public void Start() => throw new Exception(InvalidDispatchMessage);
    public void Stop() => throw new Exception(InvalidDispatchMessage);
    public void WaitForEmptyQueueAndStop() => throw new Exception(InvalidDispatchMessage);
    public void WaitForEmptyQueueAndStop(TimeSpan maxWait) => throw new Exception(InvalidDispatchMessage);
    public IEnumerable<string> ListNamedTasks() => throw new Exception(InvalidDispatchMessage);
}

/// <summary>
/// Represents an item that has not been properly configured.
/// </summary>
internal class InvalidQueueItem<T>:IWorkQueueItem<T>
{
    public bool HasItem => false;
    public T Item => throw new Exception("Tried to read from an invalid queue item. This is likely an error in the DispatchSharp library");
    public string Name => throw new Exception("Tried to read from an invalid queue item. This is likely an error in the DispatchSharp library");
    public void Finish() { }
    public void Cancel() { }
}