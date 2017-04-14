// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public class Heartbeat : IDisposable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

        private readonly IHeartbeatHandler[] _callbacks;
        private readonly TimeSpan _interval;
        private readonly ISystemClock _systemClock;
        private readonly IKestrelTrace _trace;
        private readonly Timer _timer;
        private int _executingOnHeartbeat;

        public Heartbeat(IHeartbeatHandler[] callbacks, ISystemClock systemClock, IKestrelTrace trace)
            : this(callbacks, systemClock, trace, Interval)
        {
        }

        // For testing
        internal Heartbeat(IHeartbeatHandler[] callbacks, ISystemClock systemClock, IKestrelTrace trace, TimeSpan interval)
        {
            _callbacks = callbacks;
            _interval = interval;
            _systemClock = systemClock;
            _trace = trace;
            _timer = new Timer(OnHeartbeat, state: this, dueTime: _interval, period: _interval);
        }

        private static void OnHeartbeat(object state)
        {
            ((Heartbeat)state).OnHeartbeat();
        }
        
        // Called by the Timer (background) thread
        private void OnHeartbeat()
        {
            var now = _systemClock.UtcNow;

            if (Interlocked.Exchange(ref _executingOnHeartbeat, 1) == 0)
            {
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
                _trace.TimerSlow(_interval, now);
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
