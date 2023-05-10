using System;
using System.Threading;

namespace DispatchSharp.Internal;

class SemaphoreWait: IWaitHandle
{
    private readonly SemaphoreSlim _semaphore;
    private readonly int _maxCount;

    public SemaphoreWait(int initialCount, int maxCount)
    {
        _maxCount = maxCount;
        _semaphore = new SemaphoreSlim(initialCount, _maxCount);
    }

    public bool WaitOne()
    {
        _semaphore.Wait();
        return true;
    }

    public bool WaitOne(TimeSpan timeout)
    {
        return _semaphore.Wait(timeout);
    }

    public void Set()
    {
        _semaphore.Release();
    }

    public void Reset()
    {
        throw new NotSupportedException("Reset is not supported for semaphore wait");
    }

    public bool IsSet()
    {
        return _semaphore.CurrentCount < _maxCount;
    }
}