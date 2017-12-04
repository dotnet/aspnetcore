// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class HttpConnectionTests : IDisposable
    {
        private readonly BufferPool _bufferPool;
        private readonly HttpConnectionContext _httpConnectionContext;
        private readonly HttpConnection _httpConnection;

        public HttpConnectionTests()
        {
            _bufferPool = new MemoryPool();
            var pair = PipeFactory.CreateConnectionPair(_bufferPool);

            _httpConnectionContext = new HttpConnectionContext
            {
                ConnectionId = "0123456789",
                ConnectionAdapters = new List<IConnectionAdapter>(),
                ConnectionFeatures = new FeatureCollection(),
                BufferPool = _bufferPool,
                HttpConnectionId = long.MinValue,
                Application = pair.Application,
                Transport = pair.Transport,
                ServiceContext = new TestServiceContext
                {
                    SystemClock = new SystemClock()
                }
            };

            _httpConnection = new HttpConnection(_httpConnectionContext);
        }

        public void Dispose()
        {
            _bufferPool.Dispose();
        }

        [Fact]
        public void DoesNotTimeOutWhenDebuggerIsAttached()
        {
            var mockDebugger = new Mock<IDebugger>();
            mockDebugger.SetupGet(g => g.IsAttached).Returns(true);
            _httpConnection.Debugger = mockDebugger.Object;
            _httpConnection.Initialize(_httpConnectionContext.Transport, _httpConnectionContext.Application);

            var now = DateTimeOffset.Now;
            _httpConnection.Tick(now);
            _httpConnection.SetTimeout(1, TimeoutAction.SendTimeoutResponse);
            _httpConnection.Tick(now.AddTicks(2).Add(Heartbeat.Interval));

            Assert.False(_httpConnection.RequestTimedOut);
        }

        [Fact]
        public void DoesNotTimeOutWhenRequestBodyDoesNotSatisfyMinimumDataRateButDebuggerIsAttached()
        {
            var mockDebugger = new Mock<IDebugger>();
            mockDebugger.SetupGet(g => g.IsAttached).Returns(true);
            _httpConnection.Debugger = mockDebugger.Object;
            var bytesPerSecond = 100;
            var mockLogger = new Mock<IKestrelTrace>();
            mockLogger.Setup(l => l.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>())).Throws(new InvalidOperationException("Should not log"));

            TickBodyWithMinimumDataRate(mockLogger.Object, bytesPerSecond);

            Assert.False(_httpConnection.RequestTimedOut);
        }

        [Fact]
        public void TimesOutWhenRequestBodyDoesNotSatisfyMinimumDataRate()
        {
            var bytesPerSecond = 100;
            var mockLogger = new Mock<IKestrelTrace>();
            TickBodyWithMinimumDataRate(mockLogger.Object, bytesPerSecond);

            // Timed out
            Assert.True(_httpConnection.RequestTimedOut);
            mockLogger.Verify(logger =>
                logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), bytesPerSecond), Times.Once);
        }

        private void TickBodyWithMinimumDataRate(IKestrelTrace logger, int bytesPerSecond)
        {
            var gracePeriod = TimeSpan.FromSeconds(5);

            _httpConnectionContext.ServiceContext.ServerOptions.Limits.MinRequestBodyDataRate =
                new MinDataRate(bytesPerSecond: bytesPerSecond, gracePeriod: gracePeriod);

            _httpConnectionContext.ServiceContext.Log = logger;

            _httpConnection.Initialize(_httpConnectionContext.Transport, _httpConnectionContext.Application);
            _httpConnection.Http1Connection.Reset();

            // Initialize timestamp
            var now = DateTimeOffset.UtcNow;
            _httpConnection.Tick(now);

            _httpConnection.StartTimingReads();

            // Tick after grace period w/ low data rate
            now += gracePeriod + TimeSpan.FromSeconds(1);
            _httpConnection.BytesRead(1);
            _httpConnection.Tick(now);
        }

        [Fact]
        public void RequestBodyMinimumDataRateNotEnforcedDuringGracePeriod()
        {
            var bytesPerSecond = 100;
            var gracePeriod = TimeSpan.FromSeconds(2);

            _httpConnectionContext.ServiceContext.ServerOptions.Limits.MinRequestBodyDataRate =
                new MinDataRate(bytesPerSecond: bytesPerSecond, gracePeriod: gracePeriod);

            var mockLogger = new Mock<IKestrelTrace>();
            _httpConnectionContext.ServiceContext.Log = mockLogger.Object;

            _httpConnection.Initialize(_httpConnectionContext.Transport, _httpConnectionContext.Application);
            _httpConnection.Http1Connection.Reset();

            // Initialize timestamp
            var now = DateTimeOffset.UtcNow;
            _httpConnection.Tick(now);

            _httpConnection.StartTimingReads();

            // Tick during grace period w/ low data rate
            now += TimeSpan.FromSeconds(1);
            _httpConnection.BytesRead(10);
            _httpConnection.Tick(now);

            // Not timed out
            Assert.False(_httpConnection.RequestTimedOut);
            mockLogger.Verify(logger =>
                logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), bytesPerSecond), Times.Never);

            // Tick after grace period w/ low data rate
            now += TimeSpan.FromSeconds(2);
            _httpConnection.BytesRead(10);
            _httpConnection.Tick(now);

            // Timed out
            Assert.True(_httpConnection.RequestTimedOut);
            mockLogger.Verify(logger =>
                logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), bytesPerSecond), Times.Once);
        }

        [Fact]
        public void RequestBodyDataRateIsAveragedOverTimeSpentReadingRequestBody()
        {
            var bytesPerSecond = 100;
            var gracePeriod = TimeSpan.FromSeconds(2);

            _httpConnectionContext.ServiceContext.ServerOptions.Limits.MinRequestBodyDataRate =
                new MinDataRate(bytesPerSecond: bytesPerSecond, gracePeriod: gracePeriod);

            var mockLogger = new Mock<IKestrelTrace>();
            _httpConnectionContext.ServiceContext.Log = mockLogger.Object;

            _httpConnection.Initialize(_httpConnectionContext.Transport, _httpConnectionContext.Application);
            _httpConnection.Http1Connection.Reset();

            // Initialize timestamp
            var now = DateTimeOffset.UtcNow;
            _httpConnection.Tick(now);

            _httpConnection.StartTimingReads();

            // Set base data rate to 200 bytes/second
            now += gracePeriod;
            _httpConnection.BytesRead(400);
            _httpConnection.Tick(now);

            // Data rate: 200 bytes/second
            now += TimeSpan.FromSeconds(1);
            _httpConnection.BytesRead(200);
            _httpConnection.Tick(now);

            // Not timed out
            Assert.False(_httpConnection.RequestTimedOut);
            mockLogger.Verify(logger =>
                logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), bytesPerSecond), Times.Never);

            // Data rate: 150 bytes/second
            now += TimeSpan.FromSeconds(1);
            _httpConnection.BytesRead(0);
            _httpConnection.Tick(now);

            // Not timed out
            Assert.False(_httpConnection.RequestTimedOut);
            mockLogger.Verify(logger =>
                logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), bytesPerSecond), Times.Never);

            // Data rate: 120 bytes/second
            now += TimeSpan.FromSeconds(1);
            _httpConnection.BytesRead(0);
            _httpConnection.Tick(now);

            // Not timed out
            Assert.False(_httpConnection.RequestTimedOut);
            mockLogger.Verify(logger =>
                logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), bytesPerSecond), Times.Never);

            // Data rate: 100 bytes/second
            now += TimeSpan.FromSeconds(1);
            _httpConnection.BytesRead(0);
            _httpConnection.Tick(now);

            // Not timed out
            Assert.False(_httpConnection.RequestTimedOut);
            mockLogger.Verify(logger =>
                logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), bytesPerSecond), Times.Never);

            // Data rate: ~85 bytes/second
            now += TimeSpan.FromSeconds(1);
            _httpConnection.BytesRead(0);
            _httpConnection.Tick(now);

            // Timed out
            Assert.True(_httpConnection.RequestTimedOut);
            mockLogger.Verify(logger =>
                logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), bytesPerSecond), Times.Once);
        }

        [Fact]
        public void RequestBodyDataRateNotComputedOnPausedTime()
        {
            var systemClock = new MockSystemClock();

            _httpConnectionContext.ServiceContext.ServerOptions.Limits.MinRequestBodyDataRate =
                new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(2));
            _httpConnectionContext.ServiceContext.SystemClock = systemClock;

            var mockLogger = new Mock<IKestrelTrace>();
            _httpConnectionContext.ServiceContext.Log = mockLogger.Object;

            _httpConnection.Initialize(_httpConnectionContext.Transport, _httpConnectionContext.Application);
            _httpConnection.Http1Connection.Reset();

            // Initialize timestamp
            _httpConnection.Tick(systemClock.UtcNow);

            _httpConnection.StartTimingReads();

            // Tick at 3s, expected counted time is 3s, expected data rate is 200 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(3);
            _httpConnection.BytesRead(600);
            _httpConnection.Tick(systemClock.UtcNow);

            // Pause at 3.5s
            systemClock.UtcNow += TimeSpan.FromSeconds(0.5);
            _httpConnection.PauseTimingReads();

            // Tick at 4s, expected counted time is 4s (first tick after pause goes through), expected data rate is 150 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(0.5);
            _httpConnection.Tick(systemClock.UtcNow);

            // Tick at 6s, expected counted time is 4s, expected data rate is 150 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(2);
            _httpConnection.Tick(systemClock.UtcNow);

            // Not timed out
            Assert.False(_httpConnection.RequestTimedOut);
            mockLogger.Verify(
                logger => logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()),
                Times.Never);

            // Resume at 6.5s
            systemClock.UtcNow += TimeSpan.FromSeconds(0.5);
            _httpConnection.ResumeTimingReads();

            // Tick at 9s, expected counted time is 6s, expected data rate is 100 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(1.5);
            _httpConnection.Tick(systemClock.UtcNow);

            // Not timed out
            Assert.False(_httpConnection.RequestTimedOut);
            mockLogger.Verify(
                logger => logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()),
                Times.Never);

            // Tick at 10s, expected counted time is 7s, expected data rate drops below 100 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(1);
            _httpConnection.Tick(systemClock.UtcNow);

            // Timed out
            Assert.True(_httpConnection.RequestTimedOut);
            mockLogger.Verify(
                logger => logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()),
                Times.Once);
        }

        [Fact]
        public void ReadTimingNotPausedWhenResumeCalledBeforeNextTick()
        {
            var systemClock = new MockSystemClock();

            _httpConnectionContext.ServiceContext.ServerOptions.Limits.MinRequestBodyDataRate =
                new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(2));
            _httpConnectionContext.ServiceContext.SystemClock = systemClock;

            var mockLogger = new Mock<IKestrelTrace>();
            _httpConnectionContext.ServiceContext.Log = mockLogger.Object;

            _httpConnection.Initialize(_httpConnectionContext.Transport, _httpConnectionContext.Application);
            _httpConnection.Http1Connection.Reset();

            // Initialize timestamp
            _httpConnection.Tick(systemClock.UtcNow);

            _httpConnection.StartTimingReads();

            // Tick at 2s, expected counted time is 2s, expected data rate is 100 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(2);
            _httpConnection.BytesRead(200);
            _httpConnection.Tick(systemClock.UtcNow);

            // Not timed out
            Assert.False(_httpConnection.RequestTimedOut);
            mockLogger.Verify(
                logger => logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()),
                Times.Never);

            // Pause at 2.25s
            systemClock.UtcNow += TimeSpan.FromSeconds(0.25);
            _httpConnection.PauseTimingReads();

            // Resume at 2.5s
            systemClock.UtcNow += TimeSpan.FromSeconds(0.25);
            _httpConnection.ResumeTimingReads();

            // Tick at 3s, expected counted time is 3s, expected data rate is 100 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(0.5);
            _httpConnection.BytesRead(100);
            _httpConnection.Tick(systemClock.UtcNow);

            // Not timed out
            Assert.False(_httpConnection.RequestTimedOut);
            mockLogger.Verify(
                logger => logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()),
                Times.Never);

            // Tick at 4s, expected counted time is 4s, expected data rate drops below 100 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(1);
            _httpConnection.Tick(systemClock.UtcNow);

            // Timed out
            Assert.True(_httpConnection.RequestTimedOut);
            mockLogger.Verify(
                logger => logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()),
                Times.Once);
        }

        [Fact]
        public void ReadTimingNotEnforcedWhenTimeoutIsSet()
        {
            var systemClock = new MockSystemClock();
            var timeout = TimeSpan.FromSeconds(5);

            _httpConnectionContext.ServiceContext.ServerOptions.Limits.MinRequestBodyDataRate =
                new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(2));
            _httpConnectionContext.ServiceContext.SystemClock = systemClock;

            var mockLogger = new Mock<IKestrelTrace>();
            _httpConnectionContext.ServiceContext.Log = mockLogger.Object;

            _httpConnection.Initialize(_httpConnectionContext.Transport, _httpConnectionContext.Application);
            _httpConnection.Http1Connection.Reset();

            var startTime = systemClock.UtcNow;

            // Initialize timestamp
            _httpConnection.Tick(startTime);

            _httpConnection.StartTimingReads();

            _httpConnection.SetTimeout(timeout.Ticks, TimeoutAction.StopProcessingNextRequest);

            // Tick beyond grace period with low data rate
            systemClock.UtcNow += TimeSpan.FromSeconds(3);
            _httpConnection.BytesRead(1);
            _httpConnection.Tick(systemClock.UtcNow);

            // Not timed out
            Assert.False(_httpConnection.RequestTimedOut);

            // Tick just past timeout period, adjusted by Heartbeat.Interval
            systemClock.UtcNow = startTime + timeout + Heartbeat.Interval + TimeSpan.FromTicks(1);
            _httpConnection.Tick(systemClock.UtcNow);

            // Timed out
            Assert.True(_httpConnection.RequestTimedOut);
        }

        [Fact]
        public void WriteTimingAbortsConnectionWhenWriteDoesNotCompleteWithMinimumDataRate()
        {
            var systemClock = new MockSystemClock();
            var aborted = new ManualResetEventSlim();

            _httpConnectionContext.ServiceContext.ServerOptions.Limits.MinResponseDataRate =
                new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(2));
            _httpConnectionContext.ServiceContext.SystemClock = systemClock;

            var mockLogger = new Mock<IKestrelTrace>();
            _httpConnectionContext.ServiceContext.Log = mockLogger.Object;

            _httpConnection.Initialize(_httpConnectionContext.Transport, _httpConnectionContext.Application);
            _httpConnection.Http1Connection.Reset();
            _httpConnection.Http1Connection.RequestAborted.Register(() =>
            {
                aborted.Set();
            });

            // Initialize timestamp
            _httpConnection.Tick(systemClock.UtcNow);

            // Should complete within 4 seconds, but the timeout is adjusted by adding Heartbeat.Interval
            _httpConnection.StartTimingWrite(400);

            // Tick just past 4s plus Heartbeat.Interval
            systemClock.UtcNow += TimeSpan.FromSeconds(4) + Heartbeat.Interval + TimeSpan.FromTicks(1);
            _httpConnection.Tick(systemClock.UtcNow);

            Assert.True(_httpConnection.RequestTimedOut);
            Assert.True(aborted.Wait(TimeSpan.FromSeconds(10)));
        }

        [Fact]
        public void WriteTimingAbortsConnectionWhenSmallWriteDoesNotCompleteWithinGracePeriod()
        {
            var systemClock = new MockSystemClock();
            var minResponseDataRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(5));
            var aborted = new ManualResetEventSlim();

            _httpConnectionContext.ServiceContext.ServerOptions.Limits.MinResponseDataRate = minResponseDataRate;
            _httpConnectionContext.ServiceContext.SystemClock = systemClock;

            var mockLogger = new Mock<IKestrelTrace>();
            _httpConnectionContext.ServiceContext.Log = mockLogger.Object;

            _httpConnection.Initialize(_httpConnectionContext.Transport, _httpConnectionContext.Application);
            _httpConnection.Http1Connection.Reset();
            _httpConnection.Http1Connection.RequestAborted.Register(() =>
            {
                aborted.Set();
            });

            // Initialize timestamp
            var startTime = systemClock.UtcNow;
            _httpConnection.Tick(startTime);

            // Should complete within 1 second, but the timeout is adjusted by adding Heartbeat.Interval
            _httpConnection.StartTimingWrite(100);

            // Tick just past 1s plus Heartbeat.Interval
            systemClock.UtcNow += TimeSpan.FromSeconds(1) + Heartbeat.Interval + TimeSpan.FromTicks(1);
            _httpConnection.Tick(systemClock.UtcNow);

            // Still within grace period, not timed out
            Assert.False(_httpConnection.RequestTimedOut);

            // Tick just past grace period (adjusted by Heartbeat.Interval)
            systemClock.UtcNow = startTime + minResponseDataRate.GracePeriod + Heartbeat.Interval + TimeSpan.FromTicks(1);
            _httpConnection.Tick(systemClock.UtcNow);

            Assert.True(_httpConnection.RequestTimedOut);
            Assert.True(aborted.Wait(TimeSpan.FromSeconds(10)));
        }

        [Fact]
        public void WriteTimingTimeoutPushedOnConcurrentWrite()
        {
            var systemClock = new MockSystemClock();
            var aborted = new ManualResetEventSlim();

            _httpConnectionContext.ServiceContext.ServerOptions.Limits.MinResponseDataRate =
                new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(2));
            _httpConnectionContext.ServiceContext.SystemClock = systemClock;

            var mockLogger = new Mock<IKestrelTrace>();
            _httpConnectionContext.ServiceContext.Log = mockLogger.Object;

            _httpConnection.Initialize(_httpConnectionContext.Transport, _httpConnectionContext.Application);
            _httpConnection.Http1Connection.Reset();
            _httpConnection.Http1Connection.RequestAborted.Register(() =>
            {
                aborted.Set();
            });

            // Initialize timestamp
            _httpConnection.Tick(systemClock.UtcNow);

            // Should complete within 5 seconds, but the timeout is adjusted by adding Heartbeat.Interval
            _httpConnection.StartTimingWrite(500);

            // Start a concurrent write after 3 seconds, which should complete within 3 seconds (adjusted by Heartbeat.Interval)
            _httpConnection.StartTimingWrite(300);

            // Tick just past 5s plus Heartbeat.Interval, when the first write should have completed
            systemClock.UtcNow += TimeSpan.FromSeconds(5) + Heartbeat.Interval + TimeSpan.FromTicks(1);
            _httpConnection.Tick(systemClock.UtcNow);

            // Not timed out because the timeout was pushed by the second write
            Assert.False(_httpConnection.RequestTimedOut);

            // Complete the first write, this should have no effect on the timeout
            _httpConnection.StopTimingWrite();

            // Tick just past +3s, when the second write should have completed
            systemClock.UtcNow += TimeSpan.FromSeconds(3) + TimeSpan.FromTicks(1);
            _httpConnection.Tick(systemClock.UtcNow);

            Assert.True(_httpConnection.RequestTimedOut);
            Assert.True(aborted.Wait(TimeSpan.FromSeconds(10)));
        }
    }
}
