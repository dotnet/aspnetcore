// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
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

        private async Task ConnectionClosedWhenKeepAliveTimeoutExpires(TestServer server)
        {
            using (var connection = new TestConnection(server.Port))
            {
                await connection.Send(
                    "GET / HTTP/1.1",
                    "",
                    "");
                await ReceiveResponse(connection, server.Context);

                await Task.Delay(LongDelay);

                await Assert.ThrowsAsync<IOException>(async () =>
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await ReceiveResponse(connection, server.Context);
                });
            }
        }

        private async Task ConnectionKeptAliveBetweenRequests(TestServer server)
        {
            using (var connection = new TestConnection(server.Port))
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
                    await ReceiveResponse(connection, server.Context);
                }
            }
        }

        private async Task ConnectionNotTimedOutWhileRequestBeingSent(TestServer server)
        {
            using (var connection = new TestConnection(server.Port))
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
                }

                await connection.Send(
                        "0",
                        "",
                        "");
                await ReceiveResponse(connection, server.Context);
            }
        }

        private async Task ConnectionNotTimedOutWhileAppIsRunning(TestServer server, CancellationTokenSource cts)
        {
            using (var connection = new TestConnection(server.Port))
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

                await ReceiveResponse(connection, server.Context);

                await connection.Send(
                    "GET / HTTP/1.1",
                    "",
                    "");
                await ReceiveResponse(connection, server.Context);
            }
        }

        private async Task ConnectionTimesOutWhenOpenedButNoRequestSent(TestServer server)
        {
            using (var connection = new TestConnection(server.Port))
            {
                await Task.Delay(LongDelay);
                await Assert.ThrowsAsync<IOException>(async () =>
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "");
                });
            }
        }

        private async Task KeepAliveTimeoutDoesNotApplyToUpgradedConnections(TestServer server, CancellationTokenSource cts)
        {
            using (var connection = new TestConnection(server.Port))
            {
                await connection.Send(
                    "GET /upgrade HTTP/1.1",
                    "",
                    "");
                await connection.Receive(
                    "HTTP/1.1 101 Switching Protocols",
                    "Connection: Upgrade",
                    $"Date: {server.Context.DateHeaderValue}",
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
                ServerOptions = new KestrelServerOptions
                {
                    AddServerHeader = false,
                    Limits =
                    {
                        KeepAliveTimeout = KeepAliveTimeout
                    }
                }
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

        private async Task ReceiveResponse(TestConnection connection, TestServiceContext testServiceContext)
        {
            await connection.Receive(
                "HTTP/1.1 200 OK",
                $"Date: {testServiceContext.DateHeaderValue}",
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
