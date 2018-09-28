// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public class TimeoutControl : ITimeoutControl, IConnectionTimeoutFeature
    {
        private readonly ITimeoutHandler _timeoutHandler;

        private long _lastTimestamp;
        private long _timeoutTimestamp = long.MaxValue;
        private TimeoutReason _timeoutReason;

        private readonly object _readTimingLock = new object();
        private MinDataRate _minReadRate;
        private bool _readTimingEnabled;
        private bool _readTimingPauseRequested;
        private long _readTimingElapsedTicks;
        private long _readTimingBytesRead;

        private readonly object _writeTimingLock = new object();
        private int _writeTimingWrites;
        private long _writeTimingTimeoutTimestamp;

        public TimeoutControl(ITimeoutHandler timeoutHandler)
        {
            _timeoutHandler = timeoutHandler;
        }

        internal IDebugger Debugger { get; set; } = DebuggerWrapper.Singleton;

        public void Initialize(DateTimeOffset now)
        {
            _lastTimestamp = now.Ticks;
        }

        public void Tick(DateTimeOffset now)
        {
            var timestamp = now.Ticks;

            CheckForTimeout(timestamp);
            CheckForReadDataRateTimeout(timestamp);
            CheckForWriteDataRateTimeout(timestamp);

            Interlocked.Exchange(ref _lastTimestamp, timestamp);
        }

        private void CheckForTimeout(long timestamp)
        {
            if (!Debugger.IsAttached)
            {
                if (timestamp > Interlocked.Read(ref _timeoutTimestamp))
                {
                    CancelTimeout();

                    _timeoutHandler.OnTimeout(_timeoutReason);
                }
            }
        }

        private void CheckForReadDataRateTimeout(long timestamp)
        {
            // The only time when both a timeout is set and the read data rate could be enforced is
            // when draining the request body. Since there's already a (short) timeout set for draining,
            // it's safe to not check the data rate at this point.
            if (Interlocked.Read(ref _timeoutTimestamp) != long.MaxValue)
            {
                return;
            }

            lock (_readTimingLock)
            {
                if (!_readTimingEnabled)
                {
                    return;
                }

                _readTimingElapsedTicks += timestamp - _lastTimestamp;

                if (_minReadRate.BytesPerSecond > 0 && _readTimingElapsedTicks > _minReadRate.GracePeriod.Ticks)
                {
                    var elapsedSeconds = (double)_readTimingElapsedTicks / TimeSpan.TicksPerSecond;
                    var rate = Interlocked.Read(ref _readTimingBytesRead) / elapsedSeconds;

                    if (rate < _minReadRate.BytesPerSecond && !Debugger.IsAttached)
                    {
                        _timeoutHandler.OnTimeout(TimeoutReason.ReadDataRate);
                    }
                }

                // PauseTimingReads() cannot just set _timingReads to false. It needs to go through at least one tick
                // before pausing, otherwise _readTimingElapsed might never be updated if PauseTimingReads() is always
                // called before the next tick.
                if (_readTimingPauseRequested)
                {
                    _readTimingEnabled = false;
                    _readTimingPauseRequested = false;
                }
            }
        }

        private void CheckForWriteDataRateTimeout(long timestamp)
        {
            lock (_writeTimingLock)
            {
                if (_writeTimingWrites > 0 && timestamp > _writeTimingTimeoutTimestamp && !Debugger.IsAttached)
                {
                    _timeoutHandler.OnTimeout(TimeoutReason.WriteDataRate);
                }
            }
        }

        public void SetTimeout(long ticks, TimeoutReason timeoutReason)
        {
            Debug.Assert(_timeoutTimestamp == long.MaxValue, "Concurrent timeouts are not supported");

            AssignTimeout(ticks, timeoutReason);
        }

        public void ResetTimeout(long ticks, TimeoutReason timeoutReason)
        {
            AssignTimeout(ticks, timeoutReason);
        }

        public void CancelTimeout()
        {
            Interlocked.Exchange(ref _timeoutTimestamp, long.MaxValue);
        }

        private void AssignTimeout(long ticks, TimeoutReason timeoutReason)
        {
            _timeoutReason = timeoutReason;

            // Add Heartbeat.Interval since this can be called right before the next heartbeat.
            Interlocked.Exchange(ref _timeoutTimestamp, _lastTimestamp + ticks + Heartbeat.Interval.Ticks);
        }

        public void StartTimingReads(MinDataRate minRate)
        {
            lock (_readTimingLock)
            {
                _minReadRate = minRate;
                _readTimingElapsedTicks = 0;
                _readTimingBytesRead = 0;
                _readTimingEnabled = true;
            }
        }

        public void StopTimingReads()
        {
            lock (_readTimingLock)
            {
                _readTimingEnabled = false;
            }
        }

        public void PauseTimingReads()
        {
            lock (_readTimingLock)
            {
                _readTimingPauseRequested = true;
            }
        }

        public void ResumeTimingReads()
        {
            lock (_readTimingLock)
            {
                _readTimingEnabled = true;

                // In case pause and resume were both called between ticks
                _readTimingPauseRequested = false;
            }
        }

        public void BytesRead(long count)
        {
            Interlocked.Add(ref _readTimingBytesRead, count);
        }

        public void StartTimingWrite(MinDataRate minRate, long size)
        {
            lock (_writeTimingLock)
            {
                // Add Heartbeat.Interval since this can be called right before the next heartbeat.
                var currentTimeUpperBound = _lastTimestamp + Heartbeat.Interval.Ticks;
                var ticksToCompleteWriteAtMinRate = TimeSpan.FromSeconds(size / minRate.BytesPerSecond).Ticks;

                // If ticksToCompleteWriteAtMinRate is less than the configured grace period,
                // allow that write to take up to the grace period to complete. Only add the grace period
                // to the current time and not to any accumulated timeout.
                var singleWriteTimeoutTimestamp = currentTimeUpperBound + Math.Max(
                    minRate.GracePeriod.Ticks,
                    ticksToCompleteWriteAtMinRate);

                // Don't penalize a connection for completing previous writes more quickly than required.
                // We don't want to kill a connection when flushing the chunk terminator just because the previous
                // chunk was large if the previous chunk was flushed quickly.

                // Don't add any grace period to this accumulated timeout because the grace period could
                // get accumulated repeatedly making the timeout for a bunch of consecutive small writes
                // far too conservative.
                var accumulatedWriteTimeoutTimestamp = _writeTimingTimeoutTimestamp + ticksToCompleteWriteAtMinRate;

                _writeTimingTimeoutTimestamp = Math.Max(singleWriteTimeoutTimestamp, accumulatedWriteTimeoutTimestamp);
                _writeTimingWrites++;
            }
        }

        public void StopTimingWrite()
        {
            lock (_writeTimingLock)
            {
                _writeTimingWrites--;
            }
        }

        void IConnectionTimeoutFeature.SetTimeout(TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.Zero)
            {
                throw new ArgumentException(CoreStrings.PositiveFiniteTimeSpanRequired, nameof(timeSpan));
            }
            if (_timeoutTimestamp != long.MaxValue)
            {
                throw new InvalidOperationException(CoreStrings.ConcurrentTimeoutsNotSupported);
            }

            SetTimeout(timeSpan.Ticks, TimeoutReason.TimeoutFeature);
        }

        void IConnectionTimeoutFeature.ResetTimeout(TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.Zero)
            {
                throw new ArgumentException(CoreStrings.PositiveFiniteTimeSpanRequired, nameof(timeSpan));
            }

            ResetTimeout(timeSpan.Ticks, TimeoutReason.TimeoutFeature);
        }
    }
}
