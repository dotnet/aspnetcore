// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Internal;

internal sealed class TimerAwaitable : IDisposable, ICriticalNotifyCompletion
{
    private Timer? _timer;
    private Action? _callback;
    private static readonly Action _callbackCompleted = () => { };

    private readonly TimeSpan _period;

    private readonly TimeSpan _dueTime;
    private readonly object _lockObj = new object();
    private bool _disposed;
    private bool _running = true;

    public TimerAwaitable(TimeSpan dueTime, TimeSpan period)
    {
        _dueTime = dueTime;
        _period = period;
    }

    public void Start()
    {
        if (_timer == null)
        {
            lock (_lockObj)
            {
                if (_disposed)
                {
                    return;
                }

                if (_timer == null)
                {
                    // This fixes the cycle by using a WeakReference to the state object. The object graph now looks like this:
                    // Timer -> TimerHolder -> TimerQueueTimer -> WeakReference<TimerAwaitable> -> Timer -> ...
                    // If TimerAwaitable falls out of scope, the timer should be released.
                    _timer = NonCapturingTimer.Create(state =>
                    {
                        var weakRef = (WeakReference<TimerAwaitable>)state!;
                        if (weakRef.TryGetTarget(out var thisRef))
                        {
                            thisRef.Tick();
                        }
                    },
                    state: new WeakReference<TimerAwaitable>(this),
                    dueTime: _dueTime,
                    period: _period);
                }
            }
        }
    }

    public TimerAwaitable GetAwaiter() => this;
    public bool IsCompleted => ReferenceEquals(_callback, _callbackCompleted);

    public bool GetResult()
    {
        _callback = null;

        return _running;
    }

    private void Tick()
    {
        var continuation = Interlocked.Exchange(ref _callback, _callbackCompleted);
        continuation?.Invoke();
    }

    public void OnCompleted(Action continuation)
    {
        if (ReferenceEquals(_callback, _callbackCompleted) ||
            ReferenceEquals(Interlocked.CompareExchange(ref _callback, continuation, null), _callbackCompleted))
        {
            Task.Run(continuation);
        }
    }

    public void UnsafeOnCompleted(Action continuation)
    {
        OnCompleted(continuation);
    }

    public void Stop()
    {
        lock (_lockObj)
        {
            // Stop should be used to trigger the call to end the loop which disposes
            ObjectDisposedThrowHelper.ThrowIf(_disposed, this);

            _running = false;
        }

        // Call tick here to make sure that we yield the callback,
        // if it's currently waiting, we don't need to wait for the next period
        Tick();
    }

    void IDisposable.Dispose()
    {
        lock (_lockObj)
        {
            _disposed = true;

            _timer?.Dispose();

            _timer = null;
        }
    }
}
