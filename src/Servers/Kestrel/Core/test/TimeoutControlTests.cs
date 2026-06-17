// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class TimeoutControlTests
{
    private readonly Mock<ITimeoutHandler> _mockTimeoutHandler;
    private readonly TimeoutControl _timeoutControl;
    private readonly FakeTimeProvider _timeProvider;

    public TimeoutControlTests()
    {
        _mockTimeoutHandler = new Mock<ITimeoutHandler>();
        _timeProvider = new FakeTimeProvider();
        _timeoutControl = new TimeoutControl(_mockTimeoutHandler.Object, _timeProvider);
    }

    [Fact]
    public void DoesNotTimeOutWhenDebuggerIsAttached()
    {
        var mockDebugger = new Mock<IDebugger>();
        mockDebugger.SetupGet(g => g.IsAttached).Returns(true);
        _timeoutControl.Debugger = mockDebugger.Object;

        var now = _timeProvider.GetTimestamp();
        _timeoutControl.Initialize();
        _timeoutControl.SetTimeout(TimeSpan.FromTicks(1), TimeoutReason.RequestHeaders);
        _timeoutControl.Tick(_timeProvider.GetTimestamp(now + 2, Heartbeat.Interval));

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
        var now = _timeProvider.GetTimestamp();
        _timeoutControl.Initialize();

        _timeoutControl.StartRequestBody(minRate);
        _timeoutControl.StartTimingRead();

        // Tick during grace period w/ low data rate
        now += _timeProvider.TimestampFrequency;
        _timeoutControl.BytesRead(10);
        _timeoutControl.Tick(now);

        // Not timed out
        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        // Tick after grace period w/ low data rate
        now += _timeProvider.TimestampFrequency;
        _timeoutControl.Tick(now);
        now += _timeProvider.TimestampFrequency;
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
        var now = _timeProvider.GetTimestamp();
        _timeoutControl.Initialize();

        _timeoutControl.StartRequestBody(minRate);
        _timeoutControl.StartTimingRead();

        // Set base data rate to 200 bytes/second
        now += _timeProvider.TimestampFrequency;
        _timeoutControl.Tick(now);
        now += _timeProvider.TimestampFrequency;
        _timeoutControl.BytesRead(400);
        _timeoutControl.Tick(now);

        // Data rate: 200 bytes/second
        now += _timeProvider.TimestampFrequency;
        _timeoutControl.BytesRead(200);
        _timeoutControl.Tick(now);

        // Not timed out
        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        // Data rate: 150 bytes/second
        now += _timeProvider.TimestampFrequency;
        _timeoutControl.BytesRead(0);
        _timeoutControl.Tick(now);

        // Not timed out
        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        // Data rate: 120 bytes/second
        now += _timeProvider.TimestampFrequency;
        _timeoutControl.BytesRead(0);
        _timeoutControl.Tick(now);

        // Not timed out
        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        // Data rate: 100 bytes/second
        now += _timeProvider.TimestampFrequency;
        _timeoutControl.BytesRead(0);
        _timeoutControl.Tick(now);

        // Not timed out
        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        // Data rate: ~85 bytes/second
        now += _timeProvider.TimestampFrequency;
        _timeoutControl.BytesRead(0);
        _timeoutControl.Tick(now);

        // Timed out
        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);
    }

    [Fact]
    public void RequestBodyDataRateNotComputedOnPausedTime()
    {
        var minRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(2));

        // Initialize timestamp
        _timeoutControl.Initialize();

        _timeoutControl.StartRequestBody(minRate);
        _timeoutControl.StartTimingRead();

        // Tick at 3s, expected counted time is 3s, expected data rate is 200 bytes/second
        _timeProvider.Advance(TimeSpan.FromSeconds(1));
        _timeoutControl.Tick(_timeProvider.GetTimestamp());
        _timeProvider.Advance(TimeSpan.FromSeconds(1));
        _timeoutControl.Tick(_timeProvider.GetTimestamp());
        _timeProvider.Advance(TimeSpan.FromSeconds(1));
        _timeoutControl.BytesRead(600);
        _timeoutControl.Tick(_timeProvider.GetTimestamp());

        // Pause at 3.5s
        _timeProvider.Advance(TimeSpan.FromSeconds(0.5));
        _timeoutControl.StopTimingRead();

        // Tick at 4s, expected counted time is 4s (first tick after pause goes through), expected data rate is 150 bytes/second
        _timeProvider.Advance(TimeSpan.FromSeconds(0.5));
        _timeoutControl.Tick(_timeProvider.GetTimestamp());

        // Tick at 6s, expected counted time is 4s, expected data rate is 150 bytes/second
        _timeProvider.Advance(TimeSpan.FromSeconds(2));
        _timeoutControl.Tick(_timeProvider.GetTimestamp());

        // Not timed out
        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        // Resume at 6.5s
        _timeProvider.Advance(TimeSpan.FromSeconds(0.5));
        _timeoutControl.StartTimingRead();

        // Tick at 9s, expected counted time is 6s, expected data rate is 100 bytes/second
        _timeProvider.Advance(TimeSpan.FromSeconds(1.0));
        _timeoutControl.Tick(_timeProvider.GetTimestamp());
        _timeProvider.Advance(TimeSpan.FromSeconds(.5));
        _timeoutControl.Tick(_timeProvider.GetTimestamp());

        // Not timed out
        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        // Tick at 10s, expected counted time is 7s, expected data rate drops below 100 bytes/second
        _timeProvider.Advance(TimeSpan.FromSeconds(1));
        _timeoutControl.Tick(_timeProvider.GetTimestamp());

        // Timed out
        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);
    }

    [Fact]
    public void ReadTimingNotPausedWhenResumeCalledBeforeNextTick()
    {
        var minRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(2));

        // Initialize timestamp
        _timeoutControl.Initialize();

        _timeoutControl.StartRequestBody(minRate);
        _timeoutControl.StartTimingRead();

        // Tick at 2s, expected counted time is 2s, expected data rate is 100 bytes/second
        _timeProvider.Advance(TimeSpan.FromSeconds(1));
        _timeoutControl.Tick(_timeProvider.GetTimestamp());
        _timeProvider.Advance(TimeSpan.FromSeconds(1));
        _timeoutControl.Tick(_timeProvider.GetTimestamp());
        _timeoutControl.BytesRead(200);

        // Not timed out
        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        // Pause at 2.25s
        _timeProvider.Advance(TimeSpan.FromSeconds(0.25));
        _timeoutControl.StopTimingRead();

        // Resume at 2.5s
        _timeProvider.Advance(TimeSpan.FromSeconds(0.25));
        _timeoutControl.StartTimingRead();

        // Tick at 3s, expected counted time is 3s, expected data rate is 100 bytes/second
        _timeProvider.Advance(TimeSpan.FromSeconds(0.5));
        _timeoutControl.BytesRead(100);
        _timeoutControl.Tick(_timeProvider.GetTimestamp());

        // Not timed out
        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        // Tick at 4s, expected counted time is 4s, expected data rate drops below 100 bytes/second
        _timeProvider.Advance(TimeSpan.FromSeconds(1));
        _timeoutControl.Tick(_timeProvider.GetTimestamp());

        // Timed out
        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);
    }

    [Fact]
    public void ReadTimingNotEnforcedWhenTimeoutIsSet()
    {
        var timeout = TimeSpan.FromSeconds(5);
        var minRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(2));

        // Initialize timestamp
        _timeoutControl.Initialize();

        _timeoutControl.StartRequestBody(minRate);
        _timeoutControl.StartTimingRead();

        _timeoutControl.SetTimeout(timeout, TimeoutReason.RequestBodyDrain);

        // Tick beyond grace period with low data rate
        _timeProvider.Advance(TimeSpan.FromSeconds(3));
        _timeoutControl.BytesRead(1);
        _timeoutControl.Tick(_timeProvider.GetTimestamp());

        // Not timed out
        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        // Tick just past timeout period, adjusted by Heartbeat.Interval
        _timeProvider.Advance(TimeSpan.FromSeconds(2) + Heartbeat.Interval + TimeSpan.FromTicks(1));
        _timeoutControl.Tick(_timeProvider.GetTimestamp());

        // Timed out
        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.RequestBodyDrain), Times.Once);
    }

    [Fact]
    public void ReadTimingNotEnforcedWhenLowConnectionInputFlowControlAvailability()
    {
        var minRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(2));

        var flowControl = new InputFlowControl(initialWindowSize: 2, minWindowSizeIncrement: 1);

        // Initialize timestamp
        var now = _timeProvider.GetTimestamp();
        _timeoutControl.Initialize();
        _timeoutControl.InitializeHttp2(flowControl);

        _timeoutControl.StartRequestBody(minRate);
        _timeoutControl.StartTimingRead();

        // Tick past grace period
        now += _timeProvider.TimestampFrequency;
        _timeoutControl.BytesRead(100);
        _timeoutControl.Tick(now);
        now += _timeProvider.TimestampFrequency;
        _timeoutControl.BytesRead(100);
        _timeoutControl.Tick(now);

        // Induce low flow control availability
        flowControl.TryAdvance(2);

        // Read 0 bytes in 1 second
        now += _timeProvider.TimestampFrequency;
        _timeoutControl.Tick(now);

        // Not timed out
        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        // Relieve low flow control availability
        flowControl.TryUpdateWindow(2, out _);
        _timeoutControl.Tick(now);

        // Still not timed out
        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        // Read 0 bytes in 1 second
        now += _timeProvider.TimestampFrequency;
        _timeoutControl.Tick(now);

        // Timed out
        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);
    }

    [Fact]
    public void ReadTimingOnlyCountsUpToOneHeartbeatIntervalPerTick()
    {
        var minRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(2));

        // Initialize timestamp
        var now = _timeProvider.GetTimestamp();
        _timeoutControl.Initialize();

        _timeoutControl.StartRequestBody(minRate);
        _timeoutControl.StartTimingRead();

        // Tick past grace period
        now += _timeProvider.TimestampFrequency;
        _timeoutControl.BytesRead(100);
        _timeoutControl.Tick(now);
        now += _timeProvider.TimestampFrequency;
        _timeoutControl.BytesRead(100);
        _timeoutControl.Tick(now);

        // Read 100 bytes in 2 seconds with a single tick
        now += _timeProvider.TimestampFrequency * 2;
        _timeoutControl.BytesRead(100);
        _timeoutControl.Tick(now);

        // Not timed out
        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        // Read 100 bytes in 2 seconds in two ticks
        now += _timeProvider.TimestampFrequency;
        _timeoutControl.BytesRead(100);
        _timeoutControl.Tick(now);
        now += _timeProvider.TimestampFrequency;
        _timeoutControl.Tick(now);

        // Timed out
        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);
    }

    [Fact]
    public void WriteTimingAbortsConnectionWhenWriteDoesNotCompleteWithMinimumDataRate()
    {
        var minRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(2));

        // Initialize timestamp
        _timeoutControl.Initialize();

        // Should complete within 4 seconds, but the timeout is adjusted by adding Heartbeat.Interval
        _timeoutControl.BytesWrittenToBuffer(minRate, 400);
        _timeoutControl.StartTimingWrite();

        // Tick just past 4s plus Heartbeat.Interval
        AdvanceClock(TimeSpan.FromSeconds(4) + Heartbeat.Interval + TimeSpan.FromTicks(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);
    }

    [Fact]
    public void WriteTimingAbortsConnectionWhenSmallWriteDoesNotCompleteWithinGracePeriod()
    {
        var minRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(5));

        // Initialize timestamp
        _timeoutControl.Initialize();

        // Should complete within 1 second, but the timeout is adjusted by adding Heartbeat.Interval
        _timeoutControl.BytesWrittenToBuffer(minRate, 100);
        _timeoutControl.StartTimingWrite();

        // Tick just past 1s plus Heartbeat.Interval
        AdvanceClock(TimeSpan.FromSeconds(1) + Heartbeat.Interval + TimeSpan.FromTicks(1));

        // Still within grace period, not timed out
        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        // Tick just past grace period (adjusted by Heartbeat.Interval)
        AdvanceClock(minRate.GracePeriod - TimeSpan.FromSeconds(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);
    }

    [Fact]
    public void WriteTimingTimeoutPushedOnConcurrentWrite()
    {
        var minRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(2));

        // Initialize timestamp
        _timeoutControl.Initialize();

        // Should complete within 5 seconds, but the timeout is adjusted by adding Heartbeat.Interval
        _timeoutControl.BytesWrittenToBuffer(minRate, 500);
        _timeoutControl.StartTimingWrite();

        // Start a concurrent write after 3 seconds, which should complete within 3 seconds (adjusted by Heartbeat.Interval)
        _timeoutControl.BytesWrittenToBuffer(minRate, 300);
        _timeoutControl.StartTimingWrite();

        // Tick just past 5s plus Heartbeat.Interval, when the first write should have completed
        AdvanceClock(TimeSpan.FromSeconds(5) + Heartbeat.Interval + TimeSpan.FromTicks(1));

        // Not timed out because the timeout was pushed by the second write
        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        // Complete the first write, this should have no effect on the timeout
        _timeoutControl.StopTimingWrite();

        // Tick just past +3s, when the second write should have completed
        AdvanceClock(TimeSpan.FromSeconds(3));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);
    }

    [Fact]
    public void WriteTimingAbortsConnectionWhenRepeatedSmallWritesDoNotCompleteWithMinimumDataRate()
    {
        var minRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(5));
        var numWrites = 5;
        var writeSize = 100;

        // Initialize timestamp
        _timeoutControl.Initialize();

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
        AdvanceClock(Heartbeat.Interval + minRate.GracePeriod + TimeSpan.FromSeconds((numWrites - 1) * writeSize / minRate.BytesPerSecond));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        // On more tick forward triggers the timeout.
        _timeProvider.Advance(TimeSpan.FromTicks(1));
        _timeoutControl.Tick(_timeProvider.GetTimestamp());

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);
    }

    [Fact]
    public void WriteTimingOnlyCountsUpToOneHeartbeatIntervalPerTick()
    {
        var minRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(2));

        // Initialize timestamp
        _timeoutControl.Initialize();

        // Should complete within 4 seconds, but the timeout is adjusted by adding Heartbeat.Interval
        _timeoutControl.BytesWrittenToBuffer(minRate, 400);
        _timeoutControl.StartTimingWrite();

        // Tick just past 4s plus Heartbeat.Interval at once
        _timeProvider.Advance(TimeSpan.FromSeconds(4) + Heartbeat.Interval + TimeSpan.FromTicks(1));
        _timeoutControl.Tick(_timeProvider.GetTimestamp());

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Never);

        // The last Tick only accounted for one heartbeat interval. Try again with a tick per interval.
        AdvanceClock(TimeSpan.FromSeconds(4) + TimeSpan.FromTicks(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);
    }

    private void TickBodyWithMinimumDataRate(int bytesPerSecond)
    {
        var gracePeriod = TimeSpan.FromSeconds(5);

        var minRate = new MinDataRate(bytesPerSecond, gracePeriod);

        // Initialize timestamp
        _timeoutControl.Initialize();

        _timeoutControl.StartRequestBody(minRate);
        _timeoutControl.StartTimingRead();

        AdvanceClock(gracePeriod);

        // Tick after grace period w/ low data rate
        _timeProvider.Advance(TimeSpan.FromSeconds(1));
        _timeoutControl.BytesRead(1);
        _timeoutControl.Tick(_timeProvider.GetTimestamp());
    }

    private void AdvanceClock(TimeSpan timeSpan)
    {
        var endTime = _timeProvider.GetTimestamp(timeSpan);

        while (_timeProvider.GetTimestamp(Heartbeat.Interval) < endTime)
        {
            _timeProvider.Advance(Heartbeat.Interval);
            _timeoutControl.Tick(_timeProvider.GetTimestamp());
        }

        _timeProvider.Advance(_timeProvider.GetElapsedTime(_timeProvider.GetTimestamp(), endTime));
        _timeoutControl.Tick(_timeProvider.GetTimestamp());
    }
}
