// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class HeartbeatTests
    {
        [Fact]
        public void HeartbeatIntervalIsOneSecond()
        {
            Assert.Equal(TimeSpan.FromSeconds(1), Heartbeat.Interval);
        }

        [Fact]
        public async Task BlockedHeartbeatDoesntCauseOverlapsAndIsLoggedAsError()
        {
            var systemClock = new MockSystemClock();
            var heartbeatHandler = new Mock<IHeartbeatHandler>();
            var debugger = new Mock<IDebugger>();
            var kestrelTrace = new Mock<IKestrelTrace>();
            var handlerMre = new ManualResetEventSlim();
            var handlerStartedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            heartbeatHandler.Setup(h => h.OnHeartbeat(systemClock.UtcNow)).Callback(() =>
            {
                handlerStartedTcs.SetResult(null);
                handlerMre.Wait();
            });
            debugger.Setup(d => d.IsAttached).Returns(false);

            Task blockedHeartbeatTask;

            using (var heartbeat = new Heartbeat(new[] { heartbeatHandler.Object }, systemClock, debugger.Object, kestrelTrace.Object))
            {
                blockedHeartbeatTask = Task.Run(() => heartbeat.OnHeartbeat());

                await handlerStartedTcs.Task.DefaultTimeout();

                heartbeat.OnHeartbeat();
            }

            handlerMre.Set();

            await blockedHeartbeatTask.DefaultTimeout();

            heartbeatHandler.Verify(h => h.OnHeartbeat(systemClock.UtcNow), Times.Once());
            kestrelTrace.Verify(t => t.HeartbeatSlow(TimeSpan.Zero, Heartbeat.Interval, systemClock.UtcNow), Times.Once());
        }

        [Fact]
        public async Task BlockedHeartbeatIsNotLoggedAsErrorIfDebuggerAttached()
        {
            var systemClock = new MockSystemClock();
            var heartbeatHandler = new Mock<IHeartbeatHandler>();
            var debugger = new Mock<IDebugger>();
            var kestrelTrace = new Mock<IKestrelTrace>();
            var handlerMre = new ManualResetEventSlim();
            var handlerStartedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            heartbeatHandler.Setup(h => h.OnHeartbeat(systemClock.UtcNow)).Callback(() =>
            {
                handlerStartedTcs.SetResult(null);
                handlerMre.Wait();
            });

            debugger.Setup(d => d.IsAttached).Returns(true);

            Task blockedHeartbeatTask;

            using (var heartbeat = new Heartbeat(new[] { heartbeatHandler.Object }, systemClock, debugger.Object, kestrelTrace.Object))
            {
                blockedHeartbeatTask = Task.Run(() => heartbeat.OnHeartbeat());

                await handlerStartedTcs.Task.DefaultTimeout();

                heartbeat.OnHeartbeat();
            }

            handlerMre.Set();

            await blockedHeartbeatTask.DefaultTimeout();

            heartbeatHandler.Verify(h => h.OnHeartbeat(systemClock.UtcNow), Times.Once());
            kestrelTrace.Verify(t => t.HeartbeatSlow(TimeSpan.Zero, Heartbeat.Interval, systemClock.UtcNow), Times.Never());
        }

        [Fact]
        public void ExceptionFromHeartbeatHandlerIsLoggedAsError()
        {
            var systemClock = new MockSystemClock();
            var heartbeatHandler = new Mock<IHeartbeatHandler>();
            var kestrelTrace = new TestKestrelTrace();
            var ex = new Exception();

            heartbeatHandler.Setup(h => h.OnHeartbeat(systemClock.UtcNow)).Throws(ex);

            using (var heartbeat = new Heartbeat(new[] { heartbeatHandler.Object }, systemClock, DebuggerWrapper.Singleton, kestrelTrace))
            {
                heartbeat.OnHeartbeat();
            }

            Assert.Equal(ex, kestrelTrace.Logger.Messages.Single(message => message.LogLevel == LogLevel.Error).Exception);
        }
    }
}
