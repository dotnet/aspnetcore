// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed class Heartbeat : IDisposable
{
    // Interval used by Kestrel server.
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    private readonly IHeartbeatHandler[] _callbacks;
    private readonly TimeProvider _timeProvider;
    private readonly IDebugger _debugger;
    private readonly KestrelTrace _trace;
    private readonly TimeSpan _interval;
    private readonly Thread _timerThread;
    private readonly ManualResetEventSlim _stopEvent;

    public Heartbeat(IHeartbeatHandler[] callbacks, TimeProvider timeProvider, IDebugger debugger, KestrelTrace trace, TimeSpan interval)
    {
        _callbacks = callbacks;
        _timeProvider = timeProvider;
        _debugger = debugger;
        _trace = trace;
        _interval = interval;
        // Wait time is long, so don't try to spin to exit early. Spinning would waste CPU time.
        _stopEvent = new ManualResetEventSlim(false, spinCount: 0);
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
        var now = _timeProvider.GetTimestamp();

        try
        {
            foreach (var callback in _callbacks)
            {
                callback.OnHeartbeat();
            }

            if (!_debugger.IsAttached)
            {
                var duration = _timeProvider.GetElapsedTime(now);

                if (duration > _interval)
                {
                    _trace.HeartbeatSlow(duration, _interval, _timeProvider.GetUtcNow());
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
        // Starting the heartbeat immediately triggers OnHeartbeat.
        // Initial delay to avoid running heartbeat again from timer thread.
        while (!_stopEvent.Wait(_interval))
        {
            OnHeartbeat();
        }
    }

    public void Dispose()
    {
        // Stop heart beat and immediately exit wait interval.
        _stopEvent.Set();

        // Wait for heartbeat thread to finish.
        // Should either be immediate or a short delay while heartbeat callbacks complete.
        if (_timerThread.IsAlive)
        {
            _timerThread.Join();
        }

        _stopEvent.Dispose();
    }
}
