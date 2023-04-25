namespace DispatchSharp;

/// <summary>
/// Interface for backoff/wait delays
/// </summary>
public interface IBackOffWaiter
{
    /// <summary>
    /// Wait before returning.
    /// Gives the count of consecutive waits in the process so far.
    /// </summary>
    void Wait(int count);
}