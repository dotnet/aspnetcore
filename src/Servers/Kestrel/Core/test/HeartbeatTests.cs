// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class HeartbeatTests : LoggedTest
{
    [Fact]
    public void HeartbeatIntervalIsOneSecond()
    {
        Assert.Equal(TimeSpan.FromSeconds(1), Heartbeat.Interval);
    }

    [Fact]
    public async Task HeartbeatLoopRunsWithSpecifiedInterval()
    {
        var heartbeatCallCount = 0;
        var tcs = new TaskCompletionSource();
        var timeProvider = new FakeTimeProvider();
        var heartbeatHandler = new Mock<IHeartbeatHandler>();
        var debugger = new Mock<IDebugger>();
        var kestrelTrace = new KestrelTrace(LoggerFactory);

        var splits = new List<TimeSpan>();
        Stopwatch sw = null;
        heartbeatHandler.Setup(h => h.OnHeartbeat()).Callback(() =>
        {
            heartbeatCallCount++;
            if (sw == null)
            {
                sw = Stopwatch.StartNew();
            }
            else if (heartbeatCallCount <= 5)
            {
                var split = sw.Elapsed;
                splits.Add(split);

                Logger.LogInformation($"Heartbeat split: {split.TotalMilliseconds}ms");

                sw.Restart();
            }
            else
            {
                // If shutdown takes too long there could be more OnHeartbeat calls, but that shouldn't fail the test,
                // so we ignore them. See https://github.com/dotnet/aspnetcore/issues/55297
                Logger.LogInformation("Extra OnHeartbeat call().");
            }

            if (heartbeatCallCount == 5)
            {
                Logger.LogInformation($"Heartbeat run {heartbeatCallCount} times. Notifying test.");
                tcs.SetResult();
            }
        });

        var intervalMs = 300;

        using (var heartbeat = new Heartbeat(new[] { heartbeatHandler.Object }, timeProvider, debugger.Object, kestrelTrace, TimeSpan.FromMilliseconds(intervalMs)))
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
        var timeProvider = new FakeTimeProvider();
        var heartbeatHandler = new Mock<IHeartbeatHandler>();
        var debugger = new Mock<IDebugger>();
        var kestrelTrace = new KestrelTrace(LoggerFactory);
        var handlerMre = new ManualResetEventSlim();
        var handlerStartedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var heartbeatDuration = TimeSpan.FromSeconds(2);

        heartbeatHandler.Setup(h => h.OnHeartbeat()).Callback(() =>
        {
            handlerStartedTcs.SetResult();
            handlerMre.Wait();
        });
        debugger.Setup(d => d.IsAttached).Returns(false);

        Task blockedHeartbeatTask;

        using (var heartbeat = new Heartbeat(new[] { heartbeatHandler.Object }, timeProvider, debugger.Object, kestrelTrace, Heartbeat.Interval))
        {
            blockedHeartbeatTask = Task.Run(() => heartbeat.OnHeartbeat());

            await handlerStartedTcs.Task.DefaultTimeout();
        }

        // 2 seconds passes...
        timeProvider.Advance(TimeSpan.FromSeconds(2));

        handlerMre.Set();

        await blockedHeartbeatTask.DefaultTimeout();

        heartbeatHandler.Verify(h => h.OnHeartbeat(), Times.Once());

        var warningMessage = TestSink.Writes.Single(message => message.LogLevel == LogLevel.Warning).Message;
        Assert.Equal($"As of \"{timeProvider.GetUtcNow().ToString(CultureInfo.InvariantCulture)}\", the heartbeat has been running for "
            + $"\"{heartbeatDuration.ToString("c", CultureInfo.InvariantCulture)}\" which is longer than "
            + $"\"{Heartbeat.Interval.ToString("c", CultureInfo.InvariantCulture)}\". "
            + "This could be caused by thread pool starvation.", warningMessage);
    }

    [Fact]
    public async Task HeartbeatTakingLongerThanIntervalIsNotLoggedIfDebuggerAttached()
    {
        var timeProvider = new FakeTimeProvider();
        var heartbeatHandler = new Mock<IHeartbeatHandler>();
        var debugger = new Mock<IDebugger>();
        var kestrelTrace = new KestrelTrace(LoggerFactory);
        var handlerMre = new ManualResetEventSlim();
        var handlerStartedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        heartbeatHandler.Setup(h => h.OnHeartbeat()).Callback(() =>
        {
            handlerStartedTcs.SetResult();
            handlerMre.Wait();
        });

        debugger.Setup(d => d.IsAttached).Returns(true);

        Task blockedHeartbeatTask;

        using (var heartbeat = new Heartbeat(new[] { heartbeatHandler.Object }, timeProvider, debugger.Object, kestrelTrace, Heartbeat.Interval))
        {
            blockedHeartbeatTask = Task.Run(() => heartbeat.OnHeartbeat());

            await handlerStartedTcs.Task.DefaultTimeout();
        }

        // 2 seconds passes...
        timeProvider.Advance(TimeSpan.FromSeconds(2));

        handlerMre.Set();

        await blockedHeartbeatTask.DefaultTimeout();

        heartbeatHandler.Verify(h => h.OnHeartbeat(), Times.Once());

        Assert.DoesNotContain(TestSink.Writes, w => w.EventId.Name == "HeartbeatSlow");
    }

    [Fact]
    public void ExceptionFromHeartbeatHandlerIsLoggedAsError()
    {
        var timeProvider = new FakeTimeProvider();
        var heartbeatHandler = new Mock<IHeartbeatHandler>();
        var kestrelTrace = new KestrelTrace(LoggerFactory);
        var ex = new Exception();

        heartbeatHandler.Setup(h => h.OnHeartbeat()).Throws(ex);

        using (var heartbeat = new Heartbeat(new[] { heartbeatHandler.Object }, timeProvider, DebuggerWrapper.Singleton, kestrelTrace, Heartbeat.Interval))
        {
            heartbeat.OnHeartbeat();
        }

        Assert.Equal(ex, TestSink.Writes.Single(message => message.LogLevel == LogLevel.Error).Exception);
    }
}
