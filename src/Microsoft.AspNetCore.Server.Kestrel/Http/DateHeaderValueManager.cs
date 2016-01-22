// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
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
        private volatile bool _isDisposed = false;
        private volatile bool _hadRequestsSinceLastTimerTick = false;
        private Timer _dateValueTimer;
        private long _lastRequestSeenTicks;
        private volatile bool _timerIsRunning;

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
            _dateValueTimer = new Timer(TimerLoop, state: null, dueTime: Timeout.Infinite, period: Timeout.Infinite);
        }

        /// <summary>
        /// Returns a value representing the current server date/time for use in the HTTP "Date" response header
        /// in accordance with http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.18
        /// </summary>
        /// <returns>The value.</returns>
        public virtual string GetDateHeaderValue()
        {
            _hadRequestsSinceLastTimerTick = true;
            PrepareDateValues();
            return _dateValue;
        }

        public byte[] GetDateHeaderValueBytes()
        {
            _hadRequestsSinceLastTimerTick = true;
            PrepareDateValues();
            return _activeDateBytes ? _dateBytes0 : _dateBytes1;
        }

        /// <summary>
        /// Releases all resources used by the current instance of <see cref="DateHeaderValueManager"/>.
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                lock (_timerLocker)
                {
                    if (_dateValueTimer != null)
                    {
                        _timerIsRunning = false;
                        _dateValueTimer.Dispose();
                        _dateValueTimer = null;
                    }
                }
            }
        }

        /// <summary>
        /// Starts the timer
        /// </summary>
        private void StartTimer()
        {
            var now = _systemClock.UtcNow;
            SetDateValues(now);

            if (!_isDisposed)
            {
                lock (_timerLocker)
                {
                    if (!_timerIsRunning && _dateValueTimer != null)
                    {
                        _timerIsRunning = true;
                        _dateValueTimer.Change(_timerInterval, _timerInterval);
                    }
                }
            }
        }

        /// <summary>
        /// Stops the timer
        /// </summary>
        private void StopTimer()
        {
            if (!_isDisposed)
            {
                lock (_timerLocker)
                {
                    if (_dateValueTimer != null)
                    {
                        _timerIsRunning = false;
                        _dateValueTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                }
            }
        }

        // Called by the Timer (background) thread
        private void TimerLoop(object state)
        {
            var now = _systemClock.UtcNow;

            SetDateValues(now);

            if (_hadRequestsSinceLastTimerTick)
            {
                // We served requests since the last tick, reset the flag and return as we're still active
                _hadRequestsSinceLastTimerTick = false;
                Interlocked.Exchange(ref _lastRequestSeenTicks, now.Ticks);
                return;
            }

            // No requests since the last timer tick, we need to check if we're beyond the idle threshold
            if ((now.Ticks - Interlocked.Read(ref _lastRequestSeenTicks)) >= _timeWithoutRequestsUntilIdle.Ticks)
            {
                // No requests since idle threshold so stop the timer if it's still running
                StopTimer();
            }
        }

        /// <summary>
        /// Starts the timer if it's turned off, or sets the datevalues to the current time if disposed. 
        /// </summary>
        private void PrepareDateValues()
        {
            if (_isDisposed)
            {
                SetDateValues(_systemClock.UtcNow);
            }
            else
            {
                if (!_timerIsRunning)
                {
                    StartTimer();
                }
            }
        }

        /// <summary>
        /// Sets date values from a provided ticks value
        /// </summary>
        /// <param name="value">A DateTimeOffset value</param>
        private void SetDateValues(DateTimeOffset value)
        {
            // See http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.18 for required format of Date header
            _dateValue = value.ToString(Constants.RFC1123DateFormat);
            Encoding.ASCII.GetBytes(_dateValue, 0, _dateValue.Length, !_activeDateBytes ? _dateBytes0 : _dateBytes1, "\r\nDate: ".Length);
            _activeDateBytes = !_activeDateBytes;
        }
    }
}
