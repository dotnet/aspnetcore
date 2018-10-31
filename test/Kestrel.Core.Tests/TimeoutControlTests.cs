// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class TimeoutControlTests
    {
        private readonly Mock<ITimeoutHandler> _mockTimeoutHandler;
        private readonly TimeoutControl _timeoutControl;

        public TimeoutControlTests()
        {
            _mockTimeoutHandler = new Mock<ITimeoutHandler>();
            _timeoutControl = new TimeoutControl(_mockTimeoutHandler.Object);
        }

        [Fact]
        public void DoesNotTimeOutWhenDebuggerIsAttached()
        {
            var mockDebugger = new Mock<IDebugger>();
            mockDebugger.SetupGet(g => g.IsAttached).Returns(true);
            _timeoutControl.Debugger = mockDebugger.Object;

            var now = DateTimeOffset.Now;
            _timeoutControl.Tick(now);
            _timeoutControl.SetTimeout(1, TimeoutReason.RequestHeaders);
            _timeoutControl.Tick(now.AddTicks(2).Add(Heartbeat.Interval));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);
        }


        [Fact]
        public void DoesNotTimeOutWhenRequestBodyDoesNotSatisfyMinimumDataRateButDebuggerIsAttached()
        {
            var mockDebugger = new Mock<IDebugger>();
            mockDebugger.SetupGet(g => g.IsAttached).Returns(true);
            _timeoutControl.Debugger = mockDebugger.Object;

            TickBodyWithMinimumDataRate(bytesPerSecond: 100);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);
        }

        [Fact]
        public void TimesOutWhenRequestBodyDoesNotSatisfyMinimumDataRate()
        {
            TickBodyWithMinimumDataRate(bytesPerSecond: 100);

            // Timed out
            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Once);
        }

        [Fact]
        public void RequestBodyMinimumDataRateNotEnforcedDuringGracePeriod()
        {
            var minRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(2));

            // Initialize timestamp
            var now = DateTimeOffset.UtcNow;
            _timeoutControl.Tick(now);

            _timeoutControl.StartRequestBody(minRate);
            _timeoutControl.StartTimingRead();

            // Tick during grace period w/ low data rate
            now += TimeSpan.FromSeconds(1);
            _timeoutControl.BytesRead(10);
            _timeoutControl.Tick(now);

            // Not timed out
            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            // Tick after grace period w/ low data rate
            now += TimeSpan.FromSeconds(2);
            _timeoutControl.BytesRead(10);
            _timeoutControl.Tick(now);

            // Timed out
            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);
        }

        [Fact]
        public void RequestBodyDataRateIsAveragedOverTimeSpentReadingRequestBody()
        {
            var gracePeriod = TimeSpan.FromSeconds(2);
            var minRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: gracePeriod);

            // Initialize timestamp
            var now = DateTimeOffset.UtcNow;
            _timeoutControl.Tick(now);

            _timeoutControl.StartRequestBody(minRate);
            _timeoutControl.StartTimingRead();

            // Set base data rate to 200 bytes/second
            now += gracePeriod;
            _timeoutControl.BytesRead(400);
            _timeoutControl.Tick(now);

            // Data rate: 200 bytes/second
            now += TimeSpan.FromSeconds(1);
            _timeoutControl.BytesRead(200);
            _timeoutControl.Tick(now);

            // Not timed out
            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            // Data rate: 150 bytes/second
            now += TimeSpan.FromSeconds(1);
            _timeoutControl.BytesRead(0);
            _timeoutControl.Tick(now);

            // Not timed out
            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            // Data rate: 120 bytes/second
            now += TimeSpan.FromSeconds(1);
            _timeoutControl.BytesRead(0);
            _timeoutControl.Tick(now);

            // Not timed out
            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            // Data rate: 100 bytes/second
            now += TimeSpan.FromSeconds(1);
            _timeoutControl.BytesRead(0);
            _timeoutControl.Tick(now);

            // Not timed out
            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            // Data rate: ~85 bytes/second
            now += TimeSpan.FromSeconds(1);
            _timeoutControl.BytesRead(0);
            _timeoutControl.Tick(now);

            // Timed out
            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);
        }


        [Fact]
        public void RequestBodyDataRateNotComputedOnPausedTime()
        {
            var systemClock = new MockSystemClock();
            var minRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(2));

            // Initialize timestamp
            _timeoutControl.Tick(systemClock.UtcNow);

            _timeoutControl.StartRequestBody(minRate);
            _timeoutControl.StartTimingRead();

            // Tick at 3s, expected counted time is 3s, expected data rate is 200 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(3);
            _timeoutControl.BytesRead(600);
            _timeoutControl.Tick(systemClock.UtcNow);

            // Pause at 3.5s
            systemClock.UtcNow += TimeSpan.FromSeconds(0.5);
            _timeoutControl.StopTimingRead();

            // Tick at 4s, expected counted time is 4s (first tick after pause goes through), expected data rate is 150 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(0.5);
            _timeoutControl.Tick(systemClock.UtcNow);

            // Tick at 6s, expected counted time is 4s, expected data rate is 150 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(2);
            _timeoutControl.Tick(systemClock.UtcNow);

            // Not timed out
            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            // Resume at 6.5s
            systemClock.UtcNow += TimeSpan.FromSeconds(0.5);
            _timeoutControl.StartTimingRead();

            // Tick at 9s, expected counted time is 6s, expected data rate is 100 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(1.5);
            _timeoutControl.Tick(systemClock.UtcNow);

            // Not timed out
            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            // Tick at 10s, expected counted time is 7s, expected data rate drops below 100 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(1);
            _timeoutControl.Tick(systemClock.UtcNow);

            // Timed out
            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);
        }

        [Fact]
        public void ReadTimingNotPausedWhenResumeCalledBeforeNextTick()
        {
            var systemClock = new MockSystemClock();
            var minRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(2));

            // Initialize timestamp
            _timeoutControl.Tick(systemClock.UtcNow);

            _timeoutControl.StartRequestBody(minRate);
            _timeoutControl.StartTimingRead();

            // Tick at 2s, expected counted time is 2s, expected data rate is 100 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(2);
            _timeoutControl.BytesRead(200);
            _timeoutControl.Tick(systemClock.UtcNow);

            // Not timed out
            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            // Pause at 2.25s
            systemClock.UtcNow += TimeSpan.FromSeconds(0.25);
            _timeoutControl.StopTimingRead();

            // Resume at 2.5s
            systemClock.UtcNow += TimeSpan.FromSeconds(0.25);
            _timeoutControl.StartTimingRead();

            // Tick at 3s, expected counted time is 3s, expected data rate is 100 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(0.5);
            _timeoutControl.BytesRead(100);
            _timeoutControl.Tick(systemClock.UtcNow);

            // Not timed out
            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            // Tick at 4s, expected counted time is 4s, expected data rate drops below 100 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(1);
            _timeoutControl.Tick(systemClock.UtcNow);

            // Timed out
            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);
        }

        [Fact]
        public void ReadTimingNotEnforcedWhenTimeoutIsSet()
        {
            var systemClock = new MockSystemClock();
            var timeout = TimeSpan.FromSeconds(5);
            var minRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(2));

            var startTime = systemClock.UtcNow;

            // Initialize timestamp
            _timeoutControl.Tick(startTime);

            _timeoutControl.StartRequestBody(minRate);
            _timeoutControl.StartTimingRead();

            _timeoutControl.SetTimeout(timeout.Ticks, TimeoutReason.RequestBodyDrain);

            // Tick beyond grace period with low data rate
            systemClock.UtcNow += TimeSpan.FromSeconds(3);
            _timeoutControl.BytesRead(1);
            _timeoutControl.Tick(systemClock.UtcNow);

            // Not timed out
            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            // Tick just past timeout period, adjusted by Heartbeat.Interval
            systemClock.UtcNow = startTime + timeout + Heartbeat.Interval + TimeSpan.FromTicks(1);
            _timeoutControl.Tick(systemClock.UtcNow);

            // Timed out
            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.RequestBodyDrain), Times.Once);
        }

        [Fact]
        public void WriteTimingAbortsConnectionWhenWriteDoesNotCompleteWithMinimumDataRate()
        {
            var systemClock = new MockSystemClock();
            var minRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(2));

            // Initialize timestamp
            _timeoutControl.Tick(systemClock.UtcNow);

            // Should complete within 4 seconds, but the timeout is adjusted by adding Heartbeat.Interval
            _timeoutControl.BytesWrittenToBuffer(minRate, 400);
            _timeoutControl.StartTimingWrite();

            // Tick just past 4s plus Heartbeat.Interval
            systemClock.UtcNow += TimeSpan.FromSeconds(4) + Heartbeat.Interval + TimeSpan.FromTicks(1);
            _timeoutControl.Tick(systemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);
        }

        [Fact]
        public void WriteTimingAbortsConnectionWhenSmallWriteDoesNotCompleteWithinGracePeriod()
        {
            var systemClock = new MockSystemClock();
            var minRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(5));

            // Initialize timestamp
            var startTime = systemClock.UtcNow;
            _timeoutControl.Tick(startTime);

            // Should complete within 1 second, but the timeout is adjusted by adding Heartbeat.Interval
            _timeoutControl.BytesWrittenToBuffer(minRate, 100);
            _timeoutControl.StartTimingWrite();

            // Tick just past 1s plus Heartbeat.Interval
            systemClock.UtcNow += TimeSpan.FromSeconds(1) + Heartbeat.Interval + TimeSpan.FromTicks(1);
            _timeoutControl.Tick(systemClock.UtcNow);

            // Still within grace period, not timed out
            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            // Tick just past grace period (adjusted by Heartbeat.Interval)
            systemClock.UtcNow = startTime + minRate.GracePeriod + Heartbeat.Interval + TimeSpan.FromTicks(1);
            _timeoutControl.Tick(systemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);
        }


        [Fact]
        public void WriteTimingTimeoutPushedOnConcurrentWrite()
        {
            var systemClock = new MockSystemClock();
            var minRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(2));

            // Initialize timestamp
            _timeoutControl.Tick(systemClock.UtcNow);

            // Should complete within 5 seconds, but the timeout is adjusted by adding Heartbeat.Interval
            _timeoutControl.BytesWrittenToBuffer(minRate, 500);
            _timeoutControl.StartTimingWrite();

            // Start a concurrent write after 3 seconds, which should complete within 3 seconds (adjusted by Heartbeat.Interval)
            _timeoutControl.BytesWrittenToBuffer(minRate, 300);
            _timeoutControl.StartTimingWrite();

            // Tick just past 5s plus Heartbeat.Interval, when the first write should have completed
            systemClock.UtcNow += TimeSpan.FromSeconds(5) + Heartbeat.Interval + TimeSpan.FromTicks(1);
            _timeoutControl.Tick(systemClock.UtcNow);

            // Not timed out because the timeout was pushed by the second write
            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            // Complete the first write, this should have no effect on the timeout
            _timeoutControl.StopTimingWrite();

            // Tick just past +3s, when the second write should have completed
            systemClock.UtcNow += TimeSpan.FromSeconds(3) + TimeSpan.FromTicks(1);
            _timeoutControl.Tick(systemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);
        }

        [Fact]
        public void WriteTimingAbortsConnectionWhenRepeatedSmallWritesDoNotCompleteWithMinimumDataRate()
        {
            var systemClock = new MockSystemClock();
            var minRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(5));
            var numWrites = 5;
            var writeSize = 100;

            // Initialize timestamp
            var startTime = systemClock.UtcNow;
            _timeoutControl.Tick(startTime);

            // 5 consecutive 100 byte writes.
            for (var i = 0; i < numWrites - 1; i++)
            {
                _timeoutControl.BytesWrittenToBuffer(minRate, writeSize);
            }

            // Stall the last write.
            _timeoutControl.BytesWrittenToBuffer(minRate, writeSize);
            _timeoutControl.StartTimingWrite();

            // Move the clock forward Heartbeat.Interval + MinDataRate.GracePeriod + 4 seconds.
            // The grace period should only be added for the first write. The subsequent 4 100 byte writes should add 1 second each to the timeout given the 100 byte/s min rate.
            systemClock.UtcNow += Heartbeat.Interval + minRate.GracePeriod + TimeSpan.FromSeconds((numWrites - 1) * writeSize / minRate.BytesPerSecond);
            _timeoutControl.Tick(systemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            // On more tick forward triggers the timeout.
            systemClock.UtcNow += TimeSpan.FromTicks(1);
            _timeoutControl.Tick(systemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);
        }

        private void TickBodyWithMinimumDataRate(int bytesPerSecond)
        {
            var gracePeriod = TimeSpan.FromSeconds(5);

            var minRate = new MinDataRate(bytesPerSecond, gracePeriod);

            // Initialize timestamp
            var now = DateTimeOffset.UtcNow;
            _timeoutControl.Tick(now);

            _timeoutControl.StartRequestBody(minRate);
            _timeoutControl.StartTimingRead();

            // Tick after grace period w/ low data rate
            now += gracePeriod + TimeSpan.FromSeconds(1);
            _timeoutControl.BytesRead(1);
            _timeoutControl.Tick(now);
        }
    }
}
