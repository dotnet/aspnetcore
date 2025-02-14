// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class RequestBodyTimeoutTests : LoggedTest
{
    [Fact]
    public async Task RequestTimesOutWhenRequestBodyNotReceivedAtSpecifiedMinimumRate()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var gracePeriod = TimeSpan.FromSeconds(5);
        var serviceContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

        var appRunningEvent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var server = new TestServer(context =>
        {
            context.Features.Get<IHttpMinRequestBodyDataRateFeature>().MinDataRate =
                new MinDataRate(bytesPerSecond: 1, gracePeriod: gracePeriod);

            // The server must call Request.Body.ReadAsync() *before* the test sets timeProvider.UtcNow (which is triggered by the
            // server calling appRunningEvent.SetResult(null)).  If timeProvider.UtcNow is set first, it's possible for the test to fail
            // due to the following race condition:
            //
            // 1. [test]    timeProvider.UtcNow += gracePeriod + TimeSpan.FromSeconds(1);
            // 2. [server]  Heartbeat._timer is triggered, which calls HttpConnection.Tick()
            // 3. [server]  HttpConnection.Tick() calls HttpConnection.CheckForReadDataRateTimeout()
            // 4. [server]  HttpConnection.CheckForReadDataRateTimeout() is a no-op, since _readTimingEnabled is false,
            //              since Request.Body.ReadAsync() has not been called yet
            // 5. [server]  HttpConnection.Tick() sets _lastTimestamp = timestamp
            // 6. [server]  Request.Body.ReadAsync() is called
            // 6. [test]    timeProvider.UtcNow is never updated again, so server timestamp is never updated,
            //              so HttpConnection.CheckForReadDataRateTimeout() is always a no-op until test fails
            //
            // This is a pretty tight race, since the once-per-second Heartbeat._timer needs to fire between the test updating
            // timeProvider.UtcNow and the server calling Request.Body.ReadAsync().  But it happened often enough to cause
            // test flakiness in our CI (https://github.com/aspnet/KestrelHttpServer/issues/2539).
            //
            // For verification, I was able to induce the race by adding a sleep in the RequestDelegate:
            //     appRunningEvent.SetResult(null);
            //     Thread.Sleep(5000);
            //     return context.Request.Body.ReadAsync(new byte[1], 0, 1);

            var readTask = context.Request.Body.ReadAsync(new byte[1], 0, 1);
            appRunningEvent.SetResult();
            return readTask;
        }, serviceContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: 1",
                    "",
                    "");

                await appRunningEvent.Task.DefaultTimeout();

                // Advance the timeProvider gracePeriod + TimeSpan.FromSeconds(1)
                for (var i = 0; i < 6; i++)
                {
                    serviceContext.FakeTimeProvider.Advance(TimeSpan.FromSeconds(1));
                    serviceContext.ConnectionManager.OnHeartbeat();
                }

                await connection.Receive(
                    "HTTP/1.1 408 Request Timeout",
                    "");
                await connection.ReceiveEnd(
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {serviceContext.DateHeaderValue}",
                    "",
                    "");
            }
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.MinRequestBodyDataRate, m.Tags));
    }

    [Fact]
    public async Task RequestTimesOutWhenNotDrainedWithinDrainTimeoutPeriod()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var serviceContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));
        // This test requires a real clock since we can't control when the drain timeout is set
        serviceContext.InitializeHeartbeat();

        // Ensure there's still a constant date header value.
        var timeProvider = new FakeTimeProvider();
        var date = new DateHeaderValueManager(timeProvider);
        date.OnHeartbeat();
        serviceContext.DateHeaderValueManager = date;

        var appRunningEvent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var server = new TestServer(context =>
        {
            context.Features.Get<IHttpMinRequestBodyDataRateFeature>().MinDataRate = null;

            appRunningEvent.SetResult();
            return Task.CompletedTask;
        }, serviceContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: 1",
                    "",
                    "");

                await appRunningEvent.Task.DefaultTimeout();

                // Disconnects after response completes due to the timeout
                await connection.ReceiveEnd(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {serviceContext.DateHeaderValue}",
                    "",
                    "");
            }
        }

        Assert.Contains(TestSink.Writes, w => w.EventId.Id == 32 && w.LogLevel == LogLevel.Information);
        Assert.Contains(TestSink.Writes, w => w.EventId.Id == 33 && w.LogLevel == LogLevel.Information);

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.ServerTimeout, m.Tags));
    }

    [Fact]
    public async Task ConnectionClosedEvenIfAppSwallowsException()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var gracePeriod = TimeSpan.FromSeconds(5);
        var serviceContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

        var appRunningTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var exceptionSwallowedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var server = new TestServer(async context =>
        {
            context.Features.Get<IHttpMinRequestBodyDataRateFeature>().MinDataRate =
                new MinDataRate(bytesPerSecond: 1, gracePeriod: gracePeriod);

            // See comment in RequestTimesOutWhenRequestBodyNotReceivedAtSpecifiedMinimumRate for
            // why we call ReadAsync before setting the appRunningEvent.
            var readTask = context.Request.Body.ReadAsync(new byte[1], 0, 1);
            appRunningTcs.SetResult();

            try
            {
                await readTask;
            }
            catch (Microsoft.AspNetCore.Http.BadHttpRequestException ex) when (ex.StatusCode == 408)
            {
                exceptionSwallowedTcs.SetResult();
            }
            catch (Exception ex)
            {
                exceptionSwallowedTcs.SetException(ex);
            }

            var response = "hello, world";
            context.Response.ContentLength = response.Length;
            await context.Response.WriteAsync("hello, world");
        }, serviceContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: 1",
                    "",
                    "");

                await appRunningTcs.Task.DefaultTimeout();

                // Advance the clock gracePeriod + TimeSpan.FromSeconds(1)
                for (var i = 0; i < 6; i++)
                {
                    serviceContext.FakeTimeProvider.Advance(TimeSpan.FromSeconds(1));
                    serviceContext.ConnectionManager.OnHeartbeat();
                }

                await exceptionSwallowedTcs.Task.DefaultTimeout();

                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "");
                await connection.ReceiveEnd(
                    "Content-Length: 12",
                    $"Date: {serviceContext.DateHeaderValue}",
                    "",
                    "hello, world");
            }
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.MinRequestBodyDataRate, m.Tags));
    }
}
