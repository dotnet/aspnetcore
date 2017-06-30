// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class RequestBodyTimeoutTests
    {
        [Fact]
        public async Task RequestTimesOutWhenRequestBodyNotReceivedAtSpecifiedMinimumRate()
        {
            var gracePeriod = TimeSpan.FromSeconds(5);
            var systemClock = new MockSystemClock();
            var serviceContext = new TestServiceContext
            {
                SystemClock = systemClock,
                DateHeaderValueManager = new DateHeaderValueManager(systemClock)
            };

            var appRunningEvent = new ManualResetEventSlim();

            using (var server = new TestServer(context =>
            {
                context.Features.Get<IHttpMinRequestBodyDataRateFeature>().MinDataRate =
                    new MinDataRate(bytesPerSecond: 1, gracePeriod: gracePeriod);

                appRunningEvent.Set();
                return context.Request.Body.ReadAsync(new byte[1], 0, 1);
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

                    Assert.True(appRunningEvent.Wait(TimeSpan.FromSeconds(10)));
                    systemClock.UtcNow += gracePeriod + TimeSpan.FromSeconds(1);

                    await connection.Receive(
                        "HTTP/1.1 408 Request Timeout",
                        "");
                    await connection.ReceiveForcedEnd(
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
            var systemClock = new SystemClock();
            var serviceContext = new TestServiceContext
            {
                SystemClock = systemClock,
                DateHeaderValueManager = new DateHeaderValueManager(systemClock)
            };

            var appRunningEvent = new ManualResetEventSlim();

            using (var server = new TestServer(context =>
            {
                context.Features.Get<IHttpMinRequestBodyDataRateFeature>().MinDataRate = null;

                appRunningEvent.Set();
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

                    Assert.True(appRunningEvent.Wait(TimeSpan.FromSeconds(10)));

                    await connection.Receive(
                        "HTTP/1.1 408 Request Timeout",
                        "Connection: close",
                        "");
                    await connection.ReceiveStartsWith(
                        "Date: ");
                    await connection.ReceiveForcedEnd(
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ConnectionClosedEvenIfAppSwallowsException()
        {
            var gracePeriod = TimeSpan.FromSeconds(5);
            var systemClock = new MockSystemClock();
            var serviceContext = new TestServiceContext
            {
                SystemClock = systemClock,
                DateHeaderValueManager = new DateHeaderValueManager(systemClock)
            };

            var appRunningEvent = new ManualResetEventSlim();
            var exceptionSwallowedEvent = new ManualResetEventSlim();

            using (var server = new TestServer(async context =>
            {
                context.Features.Get<IHttpMinRequestBodyDataRateFeature>().MinDataRate =
                    new MinDataRate(bytesPerSecond: 1, gracePeriod: gracePeriod);

                appRunningEvent.Set();

                try
                {
                    await context.Request.Body.ReadAsync(new byte[1], 0, 1);
                }
                catch (BadHttpRequestException ex) when (ex.StatusCode == 408)
                {
                    exceptionSwallowedEvent.Set();
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

                    Assert.True(appRunningEvent.Wait(TimeSpan.FromSeconds(10)));
                    systemClock.UtcNow += gracePeriod + TimeSpan.FromSeconds(1);
                    Assert.True(exceptionSwallowedEvent.Wait(TimeSpan.FromSeconds(10)));

                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        "");
                    await connection.ReceiveForcedEnd(
                        $"Date: {serviceContext.DateHeaderValue}",
                        "Content-Length: 12",
                        "",
                        "hello, world");
                }
            }
        }
    }
}
