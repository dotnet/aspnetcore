// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Extensions.Http
{
    // Thread-safety: We treat this class as immutable except for the timer. Creating a new object
    // for the 'expiry' pool simplifies the threading requirements significantly.
    internal class ActiveHandlerTrackingEntry
    {
        private readonly object _lock;
        private bool _timerInitialized;
        private Timer _timer;
        private TimerCallback _callback;

        public ActiveHandlerTrackingEntry(string name, LifetimeTrackingHttpMessageHandler handler, TimeSpan lifetime)
        {
            Name = name;
            Handler = handler;
            Lifetime = lifetime;

            _lock = new object();
        }

        public LifetimeTrackingHttpMessageHandler Handler { get; private set; }

        public TimeSpan Lifetime { get; }

        public string Name { get; }

        public void StartExpiryTimer(TimerCallback callback)
        {
            if (Lifetime == Timeout.InfiniteTimeSpan)
            {
                return; // never expires.
            }

            if (Volatile.Read(ref _timerInitialized))
            {
                return;
            }

            StartExpiryTimerSlow(callback);
        }

        private void StartExpiryTimerSlow(TimerCallback callback)
        {
            Debug.Assert(Lifetime != Timeout.InfiniteTimeSpan);

            lock (_lock)
            {
                if (Volatile.Read(ref _timerInitialized))
                {
                    return;
                }

                _callback = callback;
                _timer = new Timer(Timer_Tick, null, Lifetime, Timeout.InfiniteTimeSpan);

                Volatile.Write(ref _timerInitialized, true);
            }
        }

        private void Timer_Tick(object state)
        {
            Debug.Assert(_callback != null);
            Debug.Assert(_timer != null);

            lock (_lock)
            {
                _timer.Dispose();
                _timer = null;

                _callback(this);
            }
        }
    }
}
