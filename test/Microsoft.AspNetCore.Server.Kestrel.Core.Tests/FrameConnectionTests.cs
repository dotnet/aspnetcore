// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class FrameConnectionTests : IDisposable
    {
        private readonly PipeFactory _pipeFactory;
        private readonly FrameConnectionContext _frameConnectionContext;
        private readonly FrameConnection _frameConnection;

        public FrameConnectionTests()
        {
            _pipeFactory = new PipeFactory();

            _frameConnectionContext = new FrameConnectionContext
            {
                ConnectionId = "0123456789",
                ConnectionAdapters = new List<IConnectionAdapter>(),
                ConnectionInformation = new MockConnectionInformation
                {
                    PipeFactory = _pipeFactory
                },
                FrameConnectionId = long.MinValue,
                Input = _pipeFactory.Create(),
                Output = _pipeFactory.Create(),
                ServiceContext = new TestServiceContext
                {
                    SystemClock = new SystemClock()
                }
            };

            _frameConnection = new FrameConnection(_frameConnectionContext);
        }

        public void Dispose()
        {
            _pipeFactory.Dispose();
        }

        [Fact]
        public void DoesNotTimeOutWhenDebuggerIsAttached()
        {
            var mockDebugger = new Mock<IDebugger>();
            mockDebugger.SetupGet(g => g.IsAttached).Returns(true);
            _frameConnection.Debugger = mockDebugger.Object;
            _frameConnection.CreateFrame(new DummyApplication(), _frameConnectionContext.Input.Reader, _frameConnectionContext.Output);

            var now = DateTimeOffset.Now;
            _frameConnection.Tick(now);
            _frameConnection.SetTimeout(1, TimeoutAction.SendTimeoutResponse);
            _frameConnection.Tick(now.AddTicks(2).Add(Heartbeat.Interval));

            Assert.False(_frameConnection.TimedOut);
        }

        [Fact]
        public void DoesNotTimeOutWhenRequestBodyDoesNotSatisfyMinimumDataRateButDebuggerIsAttached()
        {
            var mockDebugger = new Mock<IDebugger>();
            mockDebugger.SetupGet(g => g.IsAttached).Returns(true);
            _frameConnection.Debugger = mockDebugger.Object;
            var bytesPerSecond = 100;
            var mockLogger = new Mock<IKestrelTrace>();
            mockLogger.Setup(l => l.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>())).Throws(new InvalidOperationException("Should not log"));

            TickBodyWithMinimumDataRate(mockLogger.Object, bytesPerSecond);

            Assert.False(_frameConnection.TimedOut);
        }

        [Fact]
        public void TimesOutWhenRequestBodyDoesNotSatisfyMinimumDataRate()
        {
            var bytesPerSecond = 100;
            var mockLogger = new Mock<IKestrelTrace>();
            TickBodyWithMinimumDataRate(mockLogger.Object, bytesPerSecond);

            // Timed out
            Assert.True(_frameConnection.TimedOut);
            mockLogger.Verify(logger =>
                logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), bytesPerSecond), Times.Once);
        }

        private void TickBodyWithMinimumDataRate(IKestrelTrace logger, int bytesPerSecond)
        {
            var gracePeriod = TimeSpan.FromSeconds(5);

            _frameConnectionContext.ServiceContext.ServerOptions.Limits.MinRequestBodyDataRate =
                new MinDataRate(bytesPerSecond: bytesPerSecond, gracePeriod: gracePeriod);

            _frameConnectionContext.ServiceContext.Log = logger;

            _frameConnection.CreateFrame(new DummyApplication(), _frameConnectionContext.Input.Reader, _frameConnectionContext.Output);
            _frameConnection.Frame.Reset();

            // Initialize timestamp
            var now = DateTimeOffset.UtcNow;
            _frameConnection.Tick(now);

            _frameConnection.StartTimingReads();

            // Tick after grace period w/ low data rate
            now += gracePeriod + TimeSpan.FromSeconds(1);
            _frameConnection.BytesRead(1);
            _frameConnection.Tick(now);
        }

        [Fact]
        public void RequestBodyMinimumDataRateNotEnforcedDuringGracePeriod()
        {
            var bytesPerSecond = 100;
            var gracePeriod = TimeSpan.FromSeconds(2);

            _frameConnectionContext.ServiceContext.ServerOptions.Limits.MinRequestBodyDataRate =
                new MinDataRate(bytesPerSecond: bytesPerSecond, gracePeriod: gracePeriod);

            var mockLogger = new Mock<IKestrelTrace>();
            _frameConnectionContext.ServiceContext.Log = mockLogger.Object;

            _frameConnection.CreateFrame(new DummyApplication(context => Task.CompletedTask), _frameConnectionContext.Input.Reader, _frameConnectionContext.Output);
            _frameConnection.Frame.Reset();

            // Initialize timestamp
            var now = DateTimeOffset.UtcNow;
            _frameConnection.Tick(now);

            _frameConnection.StartTimingReads();

            // Tick during grace period w/ low data rate
            now += TimeSpan.FromSeconds(1);
            _frameConnection.BytesRead(10);
            _frameConnection.Tick(now);

            // Not timed out
            Assert.False(_frameConnection.TimedOut);
            mockLogger.Verify(logger =>
                logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), bytesPerSecond), Times.Never);

            // Tick after grace period w/ low data rate
            now += TimeSpan.FromSeconds(2);
            _frameConnection.BytesRead(10);
            _frameConnection.Tick(now);

            // Timed out
            Assert.True(_frameConnection.TimedOut);
            mockLogger.Verify(logger =>
                logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), bytesPerSecond), Times.Once);
        }

        [Fact]
        public void RequestBodyDataRateIsAveragedOverTimeSpentReadingRequestBody()
        {
            var bytesPerSecond = 100;
            var gracePeriod = TimeSpan.FromSeconds(2);

            _frameConnectionContext.ServiceContext.ServerOptions.Limits.MinRequestBodyDataRate =
                new MinDataRate(bytesPerSecond: bytesPerSecond, gracePeriod: gracePeriod);

            var mockLogger = new Mock<IKestrelTrace>();
            _frameConnectionContext.ServiceContext.Log = mockLogger.Object;

            _frameConnection.CreateFrame(new DummyApplication(context => Task.CompletedTask), _frameConnectionContext.Input.Reader, _frameConnectionContext.Output);
            _frameConnection.Frame.Reset();

            // Initialize timestamp
            var now = DateTimeOffset.UtcNow;
            _frameConnection.Tick(now);

            _frameConnection.StartTimingReads();

            // Set base data rate to 200 bytes/second
            now += gracePeriod;
            _frameConnection.BytesRead(400);
            _frameConnection.Tick(now);

            // Data rate: 200 bytes/second
            now += TimeSpan.FromSeconds(1);
            _frameConnection.BytesRead(200);
            _frameConnection.Tick(now);

            // Not timed out
            Assert.False(_frameConnection.TimedOut);
            mockLogger.Verify(logger =>
                logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), bytesPerSecond), Times.Never);

            // Data rate: 150 bytes/second
            now += TimeSpan.FromSeconds(1);
            _frameConnection.BytesRead(0);
            _frameConnection.Tick(now);

            // Not timed out
            Assert.False(_frameConnection.TimedOut);
            mockLogger.Verify(logger =>
                logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), bytesPerSecond), Times.Never);

            // Data rate: 120 bytes/second
            now += TimeSpan.FromSeconds(1);
            _frameConnection.BytesRead(0);
            _frameConnection.Tick(now);

            // Not timed out
            Assert.False(_frameConnection.TimedOut);
            mockLogger.Verify(logger =>
                logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), bytesPerSecond), Times.Never);

            // Data rate: 100 bytes/second
            now += TimeSpan.FromSeconds(1);
            _frameConnection.BytesRead(0);
            _frameConnection.Tick(now);

            // Not timed out
            Assert.False(_frameConnection.TimedOut);
            mockLogger.Verify(logger =>
                logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), bytesPerSecond), Times.Never);

            // Data rate: ~85 bytes/second
            now += TimeSpan.FromSeconds(1);
            _frameConnection.BytesRead(0);
            _frameConnection.Tick(now);

            // Timed out
            Assert.True(_frameConnection.TimedOut);
            mockLogger.Verify(logger =>
                logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), bytesPerSecond), Times.Once);
        }

        [Fact]
        public void RequestBodyDataRateNotComputedOnPausedTime()
        {
            var systemClock = new MockSystemClock();

            _frameConnectionContext.ServiceContext.ServerOptions.Limits.MinRequestBodyDataRate =
                new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(2));
            _frameConnectionContext.ServiceContext.SystemClock = systemClock;

            var mockLogger = new Mock<IKestrelTrace>();
            _frameConnectionContext.ServiceContext.Log = mockLogger.Object;

            _frameConnection.CreateFrame(new DummyApplication(context => Task.CompletedTask), _frameConnectionContext.Input.Reader, _frameConnectionContext.Output);
            _frameConnection.Frame.Reset();

            // Initialize timestamp
            _frameConnection.Tick(systemClock.UtcNow);

            _frameConnection.StartTimingReads();

            // Tick at 3s, expected counted time is 3s, expected data rate is 200 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(3);
            _frameConnection.BytesRead(600);
            _frameConnection.Tick(systemClock.UtcNow);

            // Pause at 3.5s
            systemClock.UtcNow += TimeSpan.FromSeconds(0.5);
            _frameConnection.PauseTimingReads();

            // Tick at 4s, expected counted time is 4s (first tick after pause goes through), expected data rate is 150 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(0.5);
            _frameConnection.Tick(systemClock.UtcNow);

            // Tick at 6s, expected counted time is 4s, expected data rate is 150 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(2);
            _frameConnection.Tick(systemClock.UtcNow);

            // Not timed out
            Assert.False(_frameConnection.TimedOut);
            mockLogger.Verify(
                logger => logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()),
                Times.Never);

            // Resume at 6.5s
            systemClock.UtcNow += TimeSpan.FromSeconds(0.5);
            _frameConnection.ResumeTimingReads();

            // Tick at 9s, expected counted time is 6s, expected data rate is 100 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(1.5);
            _frameConnection.Tick(systemClock.UtcNow);

            // Not timed out
            Assert.False(_frameConnection.TimedOut);
            mockLogger.Verify(
                logger => logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()),
                Times.Never);

            // Tick at 10s, expected counted time is 7s, expected data rate drops below 100 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(1);
            _frameConnection.Tick(systemClock.UtcNow);

            // Timed out
            Assert.True(_frameConnection.TimedOut);
            mockLogger.Verify(
                logger => logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()),
                Times.Once);
        }

        [Fact]
        public void ReadTimingNotPausedWhenResumeCalledBeforeNextTick()
        {
            var systemClock = new MockSystemClock();

            _frameConnectionContext.ServiceContext.ServerOptions.Limits.MinRequestBodyDataRate =
                new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(2));
            _frameConnectionContext.ServiceContext.SystemClock = systemClock;

            var mockLogger = new Mock<IKestrelTrace>();
            _frameConnectionContext.ServiceContext.Log = mockLogger.Object;

            _frameConnection.CreateFrame(new DummyApplication(context => Task.CompletedTask), _frameConnectionContext.Input.Reader, _frameConnectionContext.Output);
            _frameConnection.Frame.Reset();

            // Initialize timestamp
            _frameConnection.Tick(systemClock.UtcNow);

            _frameConnection.StartTimingReads();

            // Tick at 2s, expected counted time is 2s, expected data rate is 100 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(2);
            _frameConnection.BytesRead(200);
            _frameConnection.Tick(systemClock.UtcNow);

            // Not timed out
            Assert.False(_frameConnection.TimedOut);
            mockLogger.Verify(
                logger => logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()),
                Times.Never);

            // Pause at 2.25s
            systemClock.UtcNow += TimeSpan.FromSeconds(0.25);
            _frameConnection.PauseTimingReads();

            // Resume at 2.5s
            systemClock.UtcNow += TimeSpan.FromSeconds(0.25);
            _frameConnection.ResumeTimingReads();

            // Tick at 3s, expected counted time is 3s, expected data rate is 100 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(0.5);
            _frameConnection.BytesRead(100);
            _frameConnection.Tick(systemClock.UtcNow);

            // Not timed out
            Assert.False(_frameConnection.TimedOut);
            mockLogger.Verify(
                logger => logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()),
                Times.Never);

            // Tick at 4s, expected counted time is 4s, expected data rate drops below 100 bytes/second
            systemClock.UtcNow += TimeSpan.FromSeconds(1);
            _frameConnection.Tick(systemClock.UtcNow);

            // Timed out
            Assert.True(_frameConnection.TimedOut);
            mockLogger.Verify(
                logger => logger.RequestBodyMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()),
                Times.Once);
        }
    }
}
