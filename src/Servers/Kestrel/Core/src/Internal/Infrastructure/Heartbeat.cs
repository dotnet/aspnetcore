// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal class Heartbeat : IDisposable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

        private readonly IHeartbeatHandler[] _callbacks;
        private readonly ISystemClock _systemClock;
        private readonly IDebugger _debugger;
        private readonly IKestrelTrace _trace;
        private readonly TimeSpan _interval;
        private Timer _timer;
        private int _executingOnHeartbeat;
        private long _lastHeartbeatTicks;

        public Heartbeat(IHeartbeatHandler[] callbacks, ISystemClock systemClock, IDebugger debugger, IKestrelTrace trace)
        {
            _callbacks = callbacks;
            _systemClock = systemClock;
            _debugger = debugger;
            _trace = trace;
            _interval = Interval;
        }

        public void Start()
        {
            OnHeartbeat();
            _timer = new Timer(OnHeartbeat, state: this, dueTime: _interval, period: _interval);
        }

        private static void OnHeartbeat(object state)
        {
            ((Heartbeat)state).OnHeartbeat();
        }

        // Called by the Timer (background) thread
        internal void OnHeartbeat()
        {
            var now = _systemClock.UtcNow;

            if (Interlocked.Exchange(ref _executingOnHeartbeat, 1) == 0)
            {
                Volatile.Write(ref _lastHeartbeatTicks, now.Ticks);

                try
                {
                    foreach (var callback in _callbacks)
                    {
                        callback.OnHeartbeat(now);
                    }
                }
                catch (Exception ex)
                {
                    _trace.LogError(0, ex, $"{nameof(Heartbeat)}.{nameof(OnHeartbeat)}");
                }
                finally
                {
                    Interlocked.Exchange(ref _executingOnHeartbeat, 0);
                }
            }
            else
            {
                if (!_debugger.IsAttached)
                {
                    var lastHeartbeatTicks = Volatile.Read(ref _lastHeartbeatTicks);
                    
                    _trace.HeartbeatSlow(TimeSpan.FromTicks(now.Ticks - lastHeartbeatTicks), _interval, now);
                }
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
