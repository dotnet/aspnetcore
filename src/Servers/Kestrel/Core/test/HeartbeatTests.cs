// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
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
    public async void HeartbeatLoopRunsWithSpecifiedInterval()
    {
        var heartbeatCallCount = 0;
        var tcs = new TaskCompletionSource();
        var systemClock = new MockSystemClock();
        var heartbeatHandler = new Mock<IHeartbeatHandler>();
        var debugger = new Mock<IDebugger>();
        var kestrelTrace = new KestrelTrace(LoggerFactory);
        var now = systemClock.UtcNow;

        var splits = new List<TimeSpan>();
        Stopwatch sw = null;
        heartbeatHandler.Setup(h => h.OnHeartbeat(now)).Callback(() =>
        {
            heartbeatCallCount++;
            if (sw == null)
            {
                sw = Stopwatch.StartNew();
            }
            else
            {
                var split = sw.Elapsed;
                splits.Add(split);

                Logger.LogInformation($"Heartbeat split: {split.TotalMilliseconds}ms");

                sw.Restart();
            }

            if (heartbeatCallCount == 5)
            {
                Logger.LogInformation($"Heartbeat run {heartbeatCallCount} times. Notifying test.");
                tcs.SetResult();
            }
        });

        var intervalMs = 300;

        using (var heartbeat = new Heartbeat(new[] { heartbeatHandler.Object }, systemClock, debugger.Object, kestrelTrace, TimeSpan.FromMilliseconds(intervalMs)))
        {
            heartbeat.Start();

            await tcs.Task.DefaultTimeout();
            Logger.LogInformation($"Starting heartbeat dispose.");
        }

        Assert.Collection(splits,
            ts => AssertApproxEqual(intervalMs, ts.TotalMilliseconds),
            ts => AssertApproxEqual(intervalMs, ts.TotalMilliseconds),
            ts => AssertApproxEqual(intervalMs, ts.TotalMilliseconds),
            ts => AssertApproxEqual(intervalMs, ts.TotalMilliseconds));

        static void AssertApproxEqual(double intervalMs, double actualMs)
        {
            // Interval timing isn't exact on a slow computer. For example, interval of 300ms results in split between 300ms and 450ms.
            // Under load the server might take a long time to resume. Provide tolerance for late resume.

            // Round value to nearest 50. Avoids error when wait time is slightly less than expected value.
            var roundedActualMs = Math.Round(actualMs / 50.0) * 50.0;

            if (roundedActualMs < intervalMs)
            {
                Assert.Fail($"{roundedActualMs} is smaller than wait time of {intervalMs}.");
            }
            // Be tolerant of a much larger value. Heartbeat might not immediately resume if the server is under load.
            if (roundedActualMs > intervalMs * 4)
            {
                Assert.Fail($"{roundedActualMs} is much larger than wait time of {intervalMs}.");
            }
        }
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

        using (var heartbeat = new Heartbeat(new[] { heartbeatHandler.Object }, systemClock, debugger.Object, kestrelTrace, Heartbeat.Interval))
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

        using (var heartbeat = new Heartbeat(new[] { heartbeatHandler.Object }, systemClock, debugger.Object, kestrelTrace, Heartbeat.Interval))
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

        using (var heartbeat = new Heartbeat(new[] { heartbeatHandler.Object }, systemClock, DebuggerWrapper.Singleton, kestrelTrace, Heartbeat.Interval))
        {
            heartbeat.OnHeartbeat();
        }

        Assert.Equal(ex, TestSink.Writes.Single(message => message.LogLevel == LogLevel.Error).Exception);
    }
}
