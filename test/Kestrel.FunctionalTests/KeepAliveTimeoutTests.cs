// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class KeepAliveTimeoutTests
    {
        private static readonly TimeSpan KeepAliveTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan LongDelay = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan ShortDelay = TimeSpan.FromSeconds(LongDelay.TotalSeconds / 10);

        [Fact]
        public Task TestKeepAliveTimeout()
        {
            // Delays in these tests cannot be much longer than expected.
            // Call Task.Run() to get rid of Xunit's synchronization context,
            // otherwise it can cause unexpectedly longer delays when multiple tests
            // are running in parallel. These tests becomes flaky on slower
            // hardware because the continuations for the delay tasks might take too long to be
            // scheduled if running on Xunit's synchronization context.
            return Task.Run(async () =>
            {
                var longRunningCancellationTokenSource = new CancellationTokenSource();
                var upgradeCancellationTokenSource = new CancellationTokenSource();

                using (var server = CreateServer(longRunningCancellationTokenSource.Token, upgradeCancellationTokenSource.Token))
                {
                    var tasks = new[]
                    {
                        ConnectionClosedWhenKeepAliveTimeoutExpires(server),
                        ConnectionKeptAliveBetweenRequests(server),
                        ConnectionNotTimedOutWhileRequestBeingSent(server),
                        ConnectionNotTimedOutWhileAppIsRunning(server, longRunningCancellationTokenSource),
                        ConnectionTimesOutWhenOpenedButNoRequestSent(server),
                        KeepAliveTimeoutDoesNotApplyToUpgradedConnections(server, upgradeCancellationTokenSource)
                    };

                    await Task.WhenAll(tasks);
                }
            });
        }

        private async Task ConnectionClosedWhenKeepAliveTimeoutExpires(TestServer server)
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "GET / HTTP/1.1",
                    "Host:",
                    "",
                    "");
                await ReceiveResponse(connection);
                await connection.WaitForConnectionClose().TimeoutAfter(LongDelay);
            }
        }

        private async Task ConnectionKeptAliveBetweenRequests(TestServer server)
        {
            using (var connection = server.CreateConnection())
            {
                for (var i = 0; i < 10; i++)
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await Task.Delay(ShortDelay);
                }

                for (var i = 0; i < 10; i++)
                {
                    await ReceiveResponse(connection);
                }
            }
        }

        private async Task ConnectionNotTimedOutWhileRequestBeingSent(TestServer server)
        {
            using (var connection = server.CreateConnection())
            {
                var cts = new CancellationTokenSource();
                cts.CancelAfter(LongDelay);

                await connection.Send(
                        "POST /consume HTTP/1.1",
                        "Host:",
                        "Transfer-Encoding: chunked",
                        "",
                        "");

                while (!cts.IsCancellationRequested)
                {
                    await connection.Send(
                        "1",
                        "a",
                        "");
                    await Task.Delay(ShortDelay);
                }

                await connection.Send(
                        "0",
                        "",
                        "");
                await ReceiveResponse(connection);
            }
        }

        private async Task ConnectionNotTimedOutWhileAppIsRunning(TestServer server, CancellationTokenSource cts)
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "GET /longrunning HTTP/1.1",
                    "Host:",
                    "",
                    "");
                cts.CancelAfter(LongDelay);

                while (!cts.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                }

                await ReceiveResponse(connection);

                await connection.Send(
                    "GET / HTTP/1.1",
                    "Host:",
                    "",
                    "");
                await ReceiveResponse(connection);
            }
        }

        private async Task ConnectionTimesOutWhenOpenedButNoRequestSent(TestServer server)
        {
            using (var connection = server.CreateConnection())
            {
                await Task.Delay(LongDelay);
                await connection.WaitForConnectionClose().TimeoutAfter(LongDelay);
            }
        }

        private async Task KeepAliveTimeoutDoesNotApplyToUpgradedConnections(TestServer server, CancellationTokenSource cts)
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "GET /upgrade HTTP/1.1",
                    "Host:",
                    "Connection: Upgrade",
                    "",
                    "");
                await connection.Receive(
                    "HTTP/1.1 101 Switching Protocols",
                    "Connection: Upgrade",
                    "");
                await connection.ReceiveStartsWith("Date: ");
                await connection.Receive(
                    "",
                    "");
                cts.CancelAfter(LongDelay);

                while (!cts.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                }

                await connection.Receive("hello, world");
            }
        }

        private TestServer CreateServer(CancellationToken longRunningCt, CancellationToken upgradeCt)
        {
            return new TestServer(httpContext => App(httpContext, longRunningCt, upgradeCt), new TestServiceContext
            {
                // Use real SystemClock so timeouts trigger.
                SystemClock = new SystemClock(),
                ServerOptions =
                {
                    AddServerHeader = false,
                    Limits =
                    {
                        KeepAliveTimeout = KeepAliveTimeout,
                        MinRequestBodyDataRate = null
                    }
                }
            });
        }

        private async Task App(HttpContext httpContext, CancellationToken longRunningCt, CancellationToken upgradeCt)
        {
            var ct = httpContext.RequestAborted;
            var responseStream = httpContext.Response.Body;
            var responseBytes = Encoding.ASCII.GetBytes("hello, world");

            if (httpContext.Request.Path == "/longrunning")
            {
                while (!longRunningCt.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                }
            }
            else if (httpContext.Request.Path == "/upgrade")
            {
                using (var stream = await httpContext.Features.Get<IHttpUpgradeFeature>().UpgradeAsync())
                {
                    while (!upgradeCt.IsCancellationRequested)
                    {
                        await Task.Delay(LongDelay);
                    }

                    responseStream = stream;
                }
            }
            else if (httpContext.Request.Path == "/consume")
            {
                var buffer = new byte[1024];
                while (await httpContext.Request.Body.ReadAsync(buffer, 0, buffer.Length) > 0) ;
            }

            await responseStream.WriteAsync(responseBytes, 0, responseBytes.Length);
        }

        private async Task ReceiveResponse(TestConnection connection)
        {
            await connection.Receive(
                "HTTP/1.1 200 OK",
                "");
            await connection.ReceiveStartsWith("Date: ");
            await connection.Receive(
                "Transfer-Encoding: chunked",
                "",
                "c",
                "hello, world",
                "0",
                "",
                "");
        }
    }
}
