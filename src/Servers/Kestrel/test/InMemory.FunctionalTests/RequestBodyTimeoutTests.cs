// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests
{
    public class RequestBodyTimeoutTests : LoggedTest
    {
        [Fact]
        public async Task RequestTimesOutWhenRequestBodyNotReceivedAtSpecifiedMinimumRate()
        {
            var gracePeriod = TimeSpan.FromSeconds(5);
            var serviceContext = new TestServiceContext(LoggerFactory);
            var heartbeatManager = new HeartbeatManager(serviceContext.ConnectionManager);

            var appRunningEvent = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            await using (var server = new TestServer(context =>
            {
                context.Features.Get<IHttpMinRequestBodyDataRateFeature>().MinDataRate =
                    new MinDataRate(bytesPerSecond: 1, gracePeriod: gracePeriod);

                // The server must call Request.Body.ReadAsync() *before* the test sets systemClock.UtcNow (which is triggered by the
                // server calling appRunningEvent.SetResult(null)).  If systemClock.UtcNow is set first, it's possible for the test to fail
                // due to the following race condition:
                //
                // 1. [test]    systemClock.UtcNow += gracePeriod + TimeSpan.FromSeconds(1);
                // 2. [server]  Heartbeat._timer is triggered, which calls HttpConnection.Tick()
                // 3. [server]  HttpConnection.Tick() calls HttpConnection.CheckForReadDataRateTimeout()
                // 4. [server]  HttpConnection.CheckForReadDataRateTimeout() is a no-op, since _readTimingEnabled is false,
                //              since Request.Body.ReadAsync() has not been called yet
                // 5. [server]  HttpConnection.Tick() sets _lastTimestamp = timestamp
                // 6. [server]  Request.Body.ReadAsync() is called
                // 6. [test]    systemClock.UtcNow is never updated again, so server timestamp is never updated,
                //              so HttpConnection.CheckForReadDataRateTimeout() is always a no-op until test fails
                //
                // This is a pretty tight race, since the once-per-second Heartbeat._timer needs to fire between the test updating
                // systemClock.UtcNow and the server calling Request.Body.ReadAsync().  But it happened often enough to cause
                // test flakiness in our CI (https://github.com/aspnet/KestrelHttpServer/issues/2539).
                //
                // For verification, I was able to induce the race by adding a sleep in the RequestDelegate:
                //     appRunningEvent.SetResult(null);
                //     Thread.Sleep(5000);
                //     return context.Request.Body.ReadAsync(new byte[1], 0, 1);

                var readTask = context.Request.Body.ReadAsync(new byte[1], 0, 1);
                appRunningEvent.SetResult(null);
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

                    // Advance the clock gracePeriod + TimeSpan.FromSeconds(1)
                    for (var i = 0; i < 6; i++)
                    {
                        serviceContext.MockSystemClock.UtcNow += TimeSpan.FromSeconds(1);
                        heartbeatManager.OnHeartbeat(serviceContext.SystemClock.UtcNow);
                    }

                    await connection.Receive(
                        "HTTP/1.1 408 Request Timeout",
                        "");
                    await connection.ReceiveEnd(
                        "Connection: close",
                        $"Date: {serviceContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task RequestTimesOutWhenNotDrainedWithinDrainTimeoutPeriod()
        {
            // This test requires a real clock since we can't control when the drain timeout is set
            var serviceContext = new TestServiceContext(LoggerFactory);
            serviceContext.InitializeHeartbeat();

            // Ensure there's still a constant date header value.
            var clock = new MockSystemClock();
            var date = new DateHeaderValueManager();
            date.OnHeartbeat(clock.UtcNow);
            serviceContext.DateHeaderValueManager = date;

            var appRunningEvent = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            await using (var server = new TestServer(context =>
            {
                context.Features.Get<IHttpMinRequestBodyDataRateFeature>().MinDataRate = null;

                appRunningEvent.SetResult(null);
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
                        $"Date: {serviceContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }

            Assert.Contains(TestSink.Writes, w => w.EventId.Id == 32 && w.LogLevel == LogLevel.Information);
            Assert.Contains(TestSink.Writes, w => w.EventId.Id == 33 && w.LogLevel == LogLevel.Information);
        }

        [Fact]
        public async Task ConnectionClosedEvenIfAppSwallowsException()
        {
            var gracePeriod = TimeSpan.FromSeconds(5);
            var serviceContext = new TestServiceContext(LoggerFactory);
            var heartbeatManager = new HeartbeatManager(serviceContext.ConnectionManager);

            var appRunningTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var exceptionSwallowedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            await using (var server = new TestServer(async context =>
            {
                context.Features.Get<IHttpMinRequestBodyDataRateFeature>().MinDataRate =
                    new MinDataRate(bytesPerSecond: 1, gracePeriod: gracePeriod);

                // See comment in RequestTimesOutWhenRequestBodyNotReceivedAtSpecifiedMinimumRate for
                // why we call ReadAsync before setting the appRunningEvent.
                var readTask = context.Request.Body.ReadAsync(new byte[1], 0, 1);
                appRunningTcs.SetResult(null);

                try
                {
                    await readTask;
                }
                catch (BadHttpRequestException ex) when (ex.StatusCode == 408)
                {
                    exceptionSwallowedTcs.SetResult(null);
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
                        serviceContext.MockSystemClock.UtcNow += TimeSpan.FromSeconds(1);
                        heartbeatManager.OnHeartbeat(serviceContext.SystemClock.UtcNow);
                    }

                    await exceptionSwallowedTcs.Task.DefaultTimeout();

                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        "");
                    await connection.ReceiveEnd(
                        $"Date: {serviceContext.DateHeaderValue}",
                        "Content-Length: 12",
                        "",
                        "hello, world");
                }
            }
        }
    }
}
