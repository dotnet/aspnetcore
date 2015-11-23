// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    /// <summary>
    /// Manages the generation of the date header value.
    /// </summary>
    public class DateHeaderValueManager : IDisposable
    {
        private readonly ISystemClock _systemClock;
        private readonly TimeSpan _timeWithoutRequestsUntilIdle;
        private readonly TimeSpan _timerInterval;

        private volatile string _dateValue;
        private volatile bool _activeDateBytes;
        private readonly byte[] _dateBytes0 = Encoding.ASCII.GetBytes("\r\nDate: DDD, dd mmm yyyy hh:mm:ss GMT");
        private readonly byte[] _dateBytes1 = Encoding.ASCII.GetBytes("\r\nDate: DDD, dd mmm yyyy hh:mm:ss GMT");
        private object _timerLocker = new object();
        private bool _isDisposed = false;
        private bool _hadRequestsSinceLastTimerTick = false;
        private Timer _dateValueTimer;
        private DateTimeOffset _lastRequestSeen = DateTimeOffset.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="DateHeaderValueManager"/> class.
        /// </summary>
        public DateHeaderValueManager()
            : this(
                  systemClock: new SystemClock(),
                  timeWithoutRequestsUntilIdle: TimeSpan.FromSeconds(10),
                  timerInterval: TimeSpan.FromSeconds(1))
        {

        }

        // Internal for testing
        internal DateHeaderValueManager(
            ISystemClock systemClock,
            TimeSpan timeWithoutRequestsUntilIdle,
            TimeSpan timerInterval)
        {
            _systemClock = systemClock;
            _timeWithoutRequestsUntilIdle = timeWithoutRequestsUntilIdle;
            _timerInterval = timerInterval;
        }

        /// <summary>
        /// Returns a value representing the current server date/time for use in the HTTP "Date" response header
        /// in accordance with http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.18
        /// </summary>
        /// <returns>The value.</returns>
        public virtual string GetDateHeaderValue()
        {
            PumpTimer();

            // See https://msdn.microsoft.com/en-us/library/az4se3k1(v=vs.110).aspx#RFC1123 for info on the format
            // string used here.
            // The null-coalesce here is to protect against returning null after Dispose() is called, at which
            // point _dateValue will be null forever after.
            return _dateValue ?? _systemClock.UtcNow.ToString(Constants.RFC1123DateFormat);
        }

        public byte[] GetDateHeaderValueBytes()
        {
            PumpTimer();
            return _activeDateBytes ? _dateBytes0 : _dateBytes1;
        }

        /// <summary>
        /// Releases all resources used by the current instance of <see cref="DateHeaderValueManager"/>.
        /// </summary>
        public void Dispose()
        {
            lock (_timerLocker)
            {
                DisposeTimer();
                
                _isDisposed = true;
            }
        }

        private void PumpTimer()
        {
            _hadRequestsSinceLastTimerTick = true;

            // If we're already disposed we don't care about starting the timer again. This avoids us having to worry
            // about requests in flight during dispose (not that that should actually happen) as those will just get
            // SystemClock.UtcNow (aka "the slow way").
            if (!_isDisposed && _dateValueTimer == null)
            {
                lock (_timerLocker)
                {
                    if (!_isDisposed && _dateValueTimer == null)
                    {
                        // Immediately assign the date value and start the timer again. We assign the value immediately
                        // here as the timer won't fire until the timer interval has passed and we want a value assigned
                        // inline now to serve requests that occur in the meantime.
                        _dateValue = _systemClock.UtcNow.ToString(Constants.RFC1123DateFormat);
                        Encoding.ASCII.GetBytes(_dateValue, 0, _dateValue.Length, !_activeDateBytes ? _dateBytes0 : _dateBytes1, "\r\nDate: ".Length);
                        _activeDateBytes = !_activeDateBytes;
                        _dateValueTimer = new Timer(UpdateDateValue, state: null, dueTime: _timerInterval, period: _timerInterval);
                    }
                }
            }
        }

        // Called by the Timer (background) thread
        private void UpdateDateValue(object state)
        {
            var now = _systemClock.UtcNow;

            // See http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.18 for required format of Date header
            _dateValue = now.ToString(Constants.RFC1123DateFormat);
            Encoding.ASCII.GetBytes(_dateValue, 0, _dateValue.Length, !_activeDateBytes ? _dateBytes0 : _dateBytes1, "\r\nDate: ".Length);
            _activeDateBytes = !_activeDateBytes;

            if (_hadRequestsSinceLastTimerTick)
            {
                // We served requests since the last tick, reset the flag and return as we're still active
                _hadRequestsSinceLastTimerTick = false;
                _lastRequestSeen = now;
                return;
            }

            // No requests since the last timer tick, we need to check if we're beyond the idle threshold
            var timeSinceLastRequestSeen = now - _lastRequestSeen;
            if (timeSinceLastRequestSeen >= _timeWithoutRequestsUntilIdle)
            {
                // No requests since idle threshold so stop the timer if it's still running
                if (_dateValueTimer != null)
                {
                    lock (_timerLocker)
                    {
                        if (_dateValueTimer != null)
                        {
                            DisposeTimer();
                        }
                    }
                }
            }
        }

        private void DisposeTimer()
        {
            if (_dateValueTimer != null)
            {
                _dateValueTimer.Dispose();
                _dateValueTimer = null;
                _dateValue = null;
            }
        }
    }
}
