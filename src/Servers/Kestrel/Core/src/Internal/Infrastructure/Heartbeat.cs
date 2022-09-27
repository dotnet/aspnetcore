// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed class Heartbeat : IDisposable
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    private readonly IHeartbeatHandler[] _callbacks;
    private readonly ISystemClock _systemClock;
    private readonly IDebugger _debugger;
    private readonly KestrelTrace _trace;
    private readonly TimeSpan _interval;
    private readonly Thread _timerThread;
    private volatile bool _stopped;

    public Heartbeat(IHeartbeatHandler[] callbacks, ISystemClock systemClock, IDebugger debugger, KestrelTrace trace)
    {
        _callbacks = callbacks;
        _systemClock = systemClock;
        _debugger = debugger;
        _trace = trace;
        _interval = Interval;
        _timerThread = new Thread(state => ((Heartbeat)state!).TimerLoop())
        {
            Name = "Kestrel Timer",
            IsBackground = true
        };
    }

    public void Start()
    {
        OnHeartbeat();
        _timerThread.Start(this);
    }

    internal void OnHeartbeat()
    {
        var now = _systemClock.UtcNow;

        try
        {
            foreach (var callback in _callbacks)
            {
                callback.OnHeartbeat(now);
            }

            if (!_debugger.IsAttached)
            {
                var after = _systemClock.UtcNow;

                var duration = TimeSpan.FromTicks(after.Ticks - now.Ticks);

                if (duration > _interval)
                {
                    _trace.HeartbeatSlow(duration, _interval, now);
                }
            }
        }
        catch (Exception ex)
        {
            _trace.LogError(0, ex, $"{nameof(Heartbeat)}.{nameof(OnHeartbeat)}");
        }
    }

    private void TimerLoop()
    {
        while (!_stopped)
        {
            Thread.Sleep(_interval);

            OnHeartbeat();
        }
    }

    public void Dispose()
    {
        _stopped = true;
        // Don't block waiting for the thread to exit
    }
}
