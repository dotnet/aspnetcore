// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.FunctionalTests.TestHelpers;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class KeepAliveTimeoutTests
    {
        private static readonly TimeSpan KeepAliveTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan LongDelay = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan ShortDelay = TimeSpan.FromSeconds(LongDelay.TotalSeconds / 10);

        [Fact]
        public async Task TestKeepAliveTimeout()
        {
            // Delays in these tests cannot be much longer than expected.
            // Call ConfigureAwait(false) to get rid of Xunit's synchronization context,
            // otherwise it can cause unexpectedly longer delays when multiple tests
            // are running in parallel. These tests becomes flaky on slower
            // hardware because the continuations for the delay tasks might take too long to be
            // scheduled if running on Xunit's synchronization context.
            await Task.Delay(1).ConfigureAwait(false);

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
        }

        private async Task ConnectionClosedWhenKeepAliveTimeoutExpires(TimeoutTestServer server)
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "GET / HTTP/1.1",
                    "",
                    "");
                await ReceiveResponse(connection);
                await connection.WaitForConnectionClose().TimeoutAfter(LongDelay);
            }
        }

        private async Task ConnectionKeptAliveBetweenRequests(TimeoutTestServer server)
        {
            using (var connection = server.CreateConnection())
            {
                for (var i = 0; i < 10; i++)
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
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

        private async Task ConnectionNotTimedOutWhileRequestBeingSent(TimeoutTestServer server)
        {
            using (var connection = server.CreateConnection())
            {
                var cts = new CancellationTokenSource();
                cts.CancelAfter(LongDelay);

                await connection.Send(
                        "POST / HTTP/1.1",
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

        private async Task ConnectionNotTimedOutWhileAppIsRunning(TimeoutTestServer server, CancellationTokenSource cts)
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "GET /longrunning HTTP/1.1",
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
                    "",
                    "");
                await ReceiveResponse(connection);
            }
        }

        private async Task ConnectionTimesOutWhenOpenedButNoRequestSent(TimeoutTestServer server)
        {
            using (var connection = server.CreateConnection())
            {
                await Task.Delay(LongDelay);
                await connection.WaitForConnectionClose().TimeoutAfter(LongDelay);
            }
        }

        private async Task KeepAliveTimeoutDoesNotApplyToUpgradedConnections(TimeoutTestServer server, CancellationTokenSource cts)
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "GET /upgrade HTTP/1.1",
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

        private TimeoutTestServer CreateServer(CancellationToken longRunningCt, CancellationToken upgradeCt)
        {
            return new TimeoutTestServer(httpContext => App(httpContext, longRunningCt, upgradeCt), new KestrelServerOptions
            {
                AddServerHeader = false,
                Limits = { KeepAliveTimeout = KeepAliveTimeout }
            });
        }

        private async Task App(HttpContext httpContext, CancellationToken longRunningCt, CancellationToken upgradeCt)
        {
            var ct = httpContext.RequestAborted;

            if (httpContext.Request.Path == "/longrunning")
            {
                while (!longRunningCt.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                }

                await httpContext.Response.WriteAsync("hello, world");
            }
            else if (httpContext.Request.Path == "/upgrade")
            {
                using (var stream = await httpContext.Features.Get<IHttpUpgradeFeature>().UpgradeAsync())
                {
                    while (!upgradeCt.IsCancellationRequested)
                    {
                        await Task.Delay(LongDelay);
                    }

                    var responseBytes = Encoding.ASCII.GetBytes("hello, world");
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                }
            }
            else
            {
                await httpContext.Response.WriteAsync("hello, world");
            }
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
