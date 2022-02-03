// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class HeartbeatTests : LoggedTest
{
    [Fact]
    public void HeartbeatIntervalIsOneSecond()
    {
        Assert.Equal(TimeSpan.FromSeconds(1), Heartbeat.Interval);
    }

    [Fact]
    public async Task HeartbeatTakingLongerThanIntervalIsLoggedAsWarning()
    {
        var systemClock = new MockSystemClock();
        var heartbeatHandler = new Mock<IHeartbeatHandler>();
        var debugger = new Mock<IDebugger>();
        var kestrelTrace = new KestrelTrace(LoggerFactory);
        var handlerMre = new ManualResetEventSlim();
        var handlerStartedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var now = systemClock.UtcNow;
        var heartbeatDuration = TimeSpan.FromSeconds(2);

        heartbeatHandler.Setup(h => h.OnHeartbeat(now)).Callback(() =>
        {
            handlerStartedTcs.SetResult();
            handlerMre.Wait();
        });
        debugger.Setup(d => d.IsAttached).Returns(false);

        Task blockedHeartbeatTask;

        using (var heartbeat = new Heartbeat(new[] { heartbeatHandler.Object }, systemClock, debugger.Object, kestrelTrace))
        {
            blockedHeartbeatTask = Task.Run(() => heartbeat.OnHeartbeat());

            await handlerStartedTcs.Task.DefaultTimeout();
        }

        // 2 seconds passes...
        systemClock.UtcNow = systemClock.UtcNow.AddSeconds(2);

        handlerMre.Set();

        await blockedHeartbeatTask.DefaultTimeout();

        heartbeatHandler.Verify(h => h.OnHeartbeat(now), Times.Once());

        var warningMessage = TestSink.Writes.Single(message => message.LogLevel == LogLevel.Warning).Message;
        Assert.Equal($"As of \"{now.ToString(CultureInfo.InvariantCulture)}\", the heartbeat has been running for "
            + $"\"{heartbeatDuration.ToString("c", CultureInfo.InvariantCulture)}\" which is longer than "
            + $"\"{Heartbeat.Interval.ToString("c", CultureInfo.InvariantCulture)}\". "
            + "This could be caused by thread pool starvation.", warningMessage);
    }

    [Fact]
    public async Task HeartbeatTakingLongerThanIntervalIsNotLoggedIfDebuggerAttached()
    {
        var systemClock = new MockSystemClock();
        var heartbeatHandler = new Mock<IHeartbeatHandler>();
        var debugger = new Mock<IDebugger>();
        var kestrelTrace = new KestrelTrace(LoggerFactory);
        var handlerMre = new ManualResetEventSlim();
        var handlerStartedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var now = systemClock.UtcNow;

        heartbeatHandler.Setup(h => h.OnHeartbeat(now)).Callback(() =>
        {
            handlerStartedTcs.SetResult();
            handlerMre.Wait();
        });

        debugger.Setup(d => d.IsAttached).Returns(true);

        Task blockedHeartbeatTask;

        using (var heartbeat = new Heartbeat(new[] { heartbeatHandler.Object }, systemClock, debugger.Object, kestrelTrace))
        {
            blockedHeartbeatTask = Task.Run(() => heartbeat.OnHeartbeat());

            await handlerStartedTcs.Task.DefaultTimeout();
        }

        // 2 seconds passes...
        systemClock.UtcNow = systemClock.UtcNow.AddSeconds(2);

        handlerMre.Set();

        await blockedHeartbeatTask.DefaultTimeout();

        heartbeatHandler.Verify(h => h.OnHeartbeat(now), Times.Once());

        Assert.Empty(TestSink.Writes.Where(w => w.EventId.Name == "HeartbeatSlow"));
    }

    [Fact]
    public void ExceptionFromHeartbeatHandlerIsLoggedAsError()
    {
        var systemClock = new MockSystemClock();
        var heartbeatHandler = new Mock<IHeartbeatHandler>();
        var kestrelTrace = new KestrelTrace(LoggerFactory);
        var ex = new Exception();

        heartbeatHandler.Setup(h => h.OnHeartbeat(systemClock.UtcNow)).Throws(ex);

        using (var heartbeat = new Heartbeat(new[] { heartbeatHandler.Object }, systemClock, DebuggerWrapper.Singleton, kestrelTrace))
        {
            heartbeat.OnHeartbeat();
        }

        Assert.Equal(ex, TestSink.Writes.Single(message => message.LogLevel == LogLevel.Error).Exception);
    }
}
