using System.Threading;

namespace DispatchSharp.QueueTypes;

/// <summary>
/// Linear progressive waiter. Limited to 1000ms pauses.
/// Does NOT guarantee accurate timing.
/// </summary>
public class DefaultSleeper : IBackOffWaiter
{
    /// <inheritdoc />
    public void Wait(int count)
    {
        var msDelay = count > 500 ? 1000 : count * 2;
        Thread.Sleep(msDelay);
    }
}