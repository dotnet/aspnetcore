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
        public void BlockedHeartbeatDoesntCauseOverlapsAndIsLoggedAsError()
        {
            var systemClock = new MockSystemClock();
            var heartbeatHandler = new Mock<IHeartbeatHandler>();
            var debugger = new Mock<IDebugger>();
            var kestrelTrace = new Mock<IKestrelTrace>();
            var handlerMre = new ManualResetEventSlim();
            var traceMre = new ManualResetEventSlim();
            var onHeartbeatTasks = new Task[2];

            heartbeatHandler.Setup(h => h.OnHeartbeat(systemClock.UtcNow)).Callback(() => handlerMre.Wait());
            debugger.Setup(d => d.IsAttached).Returns(false);
            kestrelTrace.Setup(t => t.HeartbeatSlow(Heartbeat.Interval, systemClock.UtcNow)).Callback(() => traceMre.Set());

            using (var heartbeat = new Heartbeat(new[] { heartbeatHandler.Object }, systemClock, debugger.Object, kestrelTrace.Object))
            {
                onHeartbeatTasks[0] = Task.Run(() => heartbeat.OnHeartbeat());
                onHeartbeatTasks[1] = Task.Run(() => heartbeat.OnHeartbeat());
                Assert.True(traceMre.Wait(TimeSpan.FromSeconds(10)));
            }

            handlerMre.Set();
            Task.WaitAll(onHeartbeatTasks);

            heartbeatHandler.Verify(h => h.OnHeartbeat(systemClock.UtcNow), Times.Once());
            kestrelTrace.Verify(t => t.HeartbeatSlow(Heartbeat.Interval, systemClock.UtcNow), Times.Once());
        }

        [Fact]
        public void BlockedHeartbeatIsNotLoggedAsErrorIfDebuggerAttached()
        {
            var systemClock = new MockSystemClock();
            var heartbeatHandler = new Mock<IHeartbeatHandler>();
            var debugger = new Mock<IDebugger>();
            var kestrelTrace = new Mock<IKestrelTrace>();
            var handlerMre = new ManualResetEventSlim();
            var traceMre = new ManualResetEventSlim();
            var onHeartbeatTasks = new Task[2];

            heartbeatHandler.Setup(h => h.OnHeartbeat(systemClock.UtcNow)).Callback(() => handlerMre.Wait());
            debugger.Setup(d => d.IsAttached).Returns(true);
            kestrelTrace.Setup(t => t.HeartbeatSlow(Heartbeat.Interval, systemClock.UtcNow)).Callback(() => traceMre.Set());

            using (var heartbeat = new Heartbeat(new[] { heartbeatHandler.Object }, systemClock, debugger.Object, kestrelTrace.Object, TimeSpan.FromSeconds(0.01)))
            {
                onHeartbeatTasks[0] = Task.Run(() => heartbeat.OnHeartbeat());
                onHeartbeatTasks[1] = Task.Run(() => heartbeat.OnHeartbeat());
                Assert.False(traceMre.Wait(TimeSpan.FromSeconds(2)));
            }

            handlerMre.Set();
            Task.WaitAll(onHeartbeatTasks);

            heartbeatHandler.Verify(h => h.OnHeartbeat(systemClock.UtcNow), Times.Once());
            kestrelTrace.Verify(t => t.HeartbeatSlow(Heartbeat.Interval, systemClock.UtcNow), Times.Never());
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
