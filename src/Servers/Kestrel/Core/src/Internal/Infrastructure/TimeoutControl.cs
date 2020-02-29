// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal class TimeoutControl : ITimeoutControl, IConnectionTimeoutFeature
    {
        private readonly ITimeoutHandler _timeoutHandler;

        private long _lastTimestamp;
        private long _timeoutTimestamp = long.MaxValue;

        private readonly object _readTimingLock = new object();
        private MinDataRate _minReadRate;
        private bool _readTimingEnabled;
        private bool _readTimingPauseRequested;
        private long _readTimingElapsedTicks;
        private long _readTimingBytesRead;
        private InputFlowControl _connectionInputFlowControl;
        // The following are always 0 or 1 for HTTP/1.x
        private int _concurrentIncompleteRequestBodies;
        private int _concurrentAwaitingReads;

        private readonly object _writeTimingLock = new object();
        private int _concurrentAwaitingWrites;
        private long _writeTimingTimeoutTimestamp;

        public TimeoutControl(ITimeoutHandler timeoutHandler)
        {
            _timeoutHandler = timeoutHandler;
        }

        public TimeoutReason TimerReason { get; private set; }

        internal IDebugger Debugger { get; set; } = DebuggerWrapper.Singleton;

        internal void Initialize(long nowTicks)
        {
            _lastTimestamp = nowTicks;
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
                    var timeoutReason = TimerReason;

                    CancelTimeout();

                    _timeoutHandler.OnTimeout(timeoutReason);
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

            // Don't enforce the rate timeout if there is back pressure due to HTTP/2 connection-level input
            // flow control. We don't consider stream-level flow control, because we wouldn't be timing a read
            // for any stream that didn't have a completely empty stream-level flow control window.
            if (_connectionInputFlowControl?.IsAvailabilityLow == true)
            {
                return;
            }

            lock (_readTimingLock)
            {
                if (!_readTimingEnabled)
                {
                    return;
                }

                // Assume overly long tick intervals are the result of server resource starvation.
                // Don't count extra time between ticks against the rate limit.
                _readTimingElapsedTicks += Math.Min(timestamp - _lastTimestamp, Heartbeat.Interval.Ticks);

                if (_minReadRate.BytesPerSecond > 0 && _readTimingElapsedTicks > _minReadRate.GracePeriod.Ticks)
                {
                    var elapsedSeconds = (double)_readTimingElapsedTicks / TimeSpan.TicksPerSecond;
                    var rate = _readTimingBytesRead / elapsedSeconds;

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
                // Assume overly long tick intervals are the result of server resource starvation.
                // Don't count extra time between ticks against the rate limit.
                var extraTimeForTick = timestamp - _lastTimestamp - Heartbeat.Interval.Ticks;

                if (extraTimeForTick > 0)
                {
                    _writeTimingTimeoutTimestamp += extraTimeForTick;
                }

                if (_concurrentAwaitingWrites > 0 && timestamp > _writeTimingTimeoutTimestamp && !Debugger.IsAttached)
                {
                    _timeoutHandler.OnTimeout(TimeoutReason.WriteDataRate);
                }
            }
        }

        public void SetTimeout(long ticks, TimeoutReason timeoutReason)
        {
            Debug.Assert(_timeoutTimestamp == long.MaxValue, "Concurrent timeouts are not supported.");

            AssignTimeout(ticks, timeoutReason);
        }

        public void ResetTimeout(long ticks, TimeoutReason timeoutReason)
        {
            AssignTimeout(ticks, timeoutReason);
        }

        public void CancelTimeout()
        {
            Interlocked.Exchange(ref _timeoutTimestamp, long.MaxValue);

            TimerReason = TimeoutReason.None;
        }

        private void AssignTimeout(long ticks, TimeoutReason timeoutReason)
        {
            TimerReason = timeoutReason;

            // Add Heartbeat.Interval since this can be called right before the next heartbeat.
            Interlocked.Exchange(ref _timeoutTimestamp, Interlocked.Read(ref _lastTimestamp) + ticks + Heartbeat.Interval.Ticks);
        }

        public void InitializeHttp2(InputFlowControl connectionInputFlowControl)
        {
            _connectionInputFlowControl = connectionInputFlowControl;
        }

        public void StartRequestBody(MinDataRate minRate)
        {
            lock (_readTimingLock)
            {
                // minRate is always KestrelServerLimits.MinRequestBodyDataRate for HTTP/2 which is the only protocol that supports concurrent request bodies.
                Debug.Assert(_concurrentIncompleteRequestBodies == 0 || minRate == _minReadRate, "Multiple simultaneous read data rates are not supported.");

                _minReadRate = minRate;
                _concurrentIncompleteRequestBodies++;

                if (_concurrentIncompleteRequestBodies == 1)
                {
                    _readTimingElapsedTicks = 0;
                    _readTimingBytesRead = 0;
                }
            }
        }

        public void StopRequestBody()
        {
            lock (_readTimingLock)
            {
                _concurrentIncompleteRequestBodies--;

                if (_concurrentIncompleteRequestBodies == 0)
                {
                    _readTimingEnabled = false;
                }
            }
        }

        public void StopTimingRead()
        {
            lock (_readTimingLock)
            {
                _concurrentAwaitingReads--;

                if (_concurrentAwaitingReads == 0)
                {
                    _readTimingPauseRequested = true;
                }
            }
        }

        public void StartTimingRead()
        {
            lock (_readTimingLock)
            {
                _concurrentAwaitingReads++;

                _readTimingEnabled = true;

                // In case pause and resume were both called between ticks
                _readTimingPauseRequested = false;
            }
        }

        public void BytesRead(long count)
        {
            Debug.Assert(count >= 0, "BytesRead count must not be negative.");

            lock (_readTimingLock)
            {
                _readTimingBytesRead += count;
            }
        }

        public void StartTimingWrite()
        {
            lock (_writeTimingLock)
            {
                _concurrentAwaitingWrites++;
            }
        }

        public void StopTimingWrite()
        {
            lock (_writeTimingLock)
            {
                _concurrentAwaitingWrites--;
            }
        }

        public void BytesWrittenToBuffer(MinDataRate minRate, long count)
        {
            lock (_writeTimingLock)
            {
                // Add Heartbeat.Interval since this can be called right before the next heartbeat.
                var currentTimeUpperBound = Interlocked.Read(ref _lastTimestamp) + Heartbeat.Interval.Ticks;
                var ticksToCompleteWriteAtMinRate = TimeSpan.FromSeconds(count / minRate.BytesPerSecond).Ticks;

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
