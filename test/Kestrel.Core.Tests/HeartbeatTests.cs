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
            var kestrelTrace = new Mock<IKestrelTrace>();
            var handlerMre = new ManualResetEventSlim();
            var traceMre = new ManualResetEventSlim();

            heartbeatHandler.Setup(h => h.OnHeartbeat(systemClock.UtcNow)).Callback(() => handlerMre.Wait());
            kestrelTrace.Setup(t => t.HeartbeatSlow(Heartbeat.Interval, systemClock.UtcNow)).Callback(() => traceMre.Set());

            using (var heartbeat = new Heartbeat(new[] { heartbeatHandler.Object }, systemClock, kestrelTrace.Object))
            {
                Task.Run(() => heartbeat.OnHeartbeat());
                Task.Run(() => heartbeat.OnHeartbeat());
                Assert.True(traceMre.Wait(TimeSpan.FromSeconds(10)));
            }

            handlerMre.Set();

            heartbeatHandler.Verify(h => h.OnHeartbeat(systemClock.UtcNow), Times.Once());
            kestrelTrace.Verify(t => t.HeartbeatSlow(Heartbeat.Interval, systemClock.UtcNow), Times.Once());
        }

        [Fact]
        public void ExceptionFromHeartbeatHandlerIsLoggedAsError()
        {
            var systemClock = new MockSystemClock();
            var heartbeatHandler = new Mock<IHeartbeatHandler>();
            var kestrelTrace = new TestKestrelTrace();
            var ex = new Exception();

            heartbeatHandler.Setup(h => h.OnHeartbeat(systemClock.UtcNow)).Throws(ex);

            using (var heartbeat = new Heartbeat(new[] { heartbeatHandler.Object }, systemClock, kestrelTrace))
            {
                heartbeat.OnHeartbeat();
            }

            Assert.Equal(ex, kestrelTrace.Logger.Messages.Single(message => message.LogLevel == LogLevel.Error).Exception);
        }
    }
}
