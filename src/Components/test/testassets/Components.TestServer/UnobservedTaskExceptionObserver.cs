// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Threading;

namespace TestServer;

public class UnobservedTaskExceptionObserver
{
    private readonly ConcurrentQueue<Exception> _exceptions = new();
    private int _circularRedirectCount;

    public void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        _exceptions.Enqueue(e.Exception);
        e.SetObserved(); // Mark as observed to prevent the process from crashing during tests
    }

    public bool HasExceptions => !_exceptions.IsEmpty;

    public IReadOnlyCollection<Exception> GetExceptions() => _exceptions.ToArray();

    public void Clear()
    {
        _exceptions.Clear();
        _circularRedirectCount = 0;
    }

    public int GetCircularRedirectCount()
    {
        return _circularRedirectCount;
    }

    public int GetAndIncrementCircularRedirectCount()
    {
        return Interlocked.Increment(ref _circularRedirectCount) - 1;
    }
}
