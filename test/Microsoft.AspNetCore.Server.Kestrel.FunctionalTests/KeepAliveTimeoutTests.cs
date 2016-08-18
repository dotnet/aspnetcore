// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class KeepAliveTimeoutTests
    {
        private static readonly TimeSpan KeepAliveTimeout = TimeSpan.FromSeconds(10);
        private static readonly int LongDelay = (int)TimeSpan.FromSeconds(30).TotalMilliseconds;
        private static readonly int ShortDelay = LongDelay / 10;

        [Fact]
        public async Task TestKeepAliveTimeout()
        {
            using (var server = CreateServer())
            {
                var tasks = new[]
                {
                    ConnectionClosedWhenKeepAliveTimeoutExpires(server),
                    ConnectionClosedWhenKeepAliveTimeoutExpiresAfterChunkedRequest(server),
                    KeepAliveTimeoutResetsBetweenContentLengthRequests(server),
                    KeepAliveTimeoutResetsBetweenChunkedRequests(server),
                    KeepAliveTimeoutNotTriggeredMidContentLengthRequest(server),
                    KeepAliveTimeoutNotTriggeredMidChunkedRequest(server),
                    ConnectionTimesOutWhenOpenedButNoRequestSent(server)
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

        private async Task ConnectionClosedWhenKeepAliveTimeoutExpiresAfterChunkedRequest(TestServer server)
        {
            using (var connection = new TestConnection(server.Port))
            {
                await connection.Send(
                        "POST / HTTP/1.1",
                        "Transfer-Encoding: chunked",
                        "",
                        "5", "hello",
                        "6", " world",
                        "0",
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

        private async Task KeepAliveTimeoutResetsBetweenContentLengthRequests(TestServer server)
        {
            using (var connection = new TestConnection(server.Port))
            {
                for (var i = 0; i < 10; i++)
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await ReceiveResponse(connection, server.Context);
                    await Task.Delay(ShortDelay);
                }
            }
        }

        private async Task KeepAliveTimeoutResetsBetweenChunkedRequests(TestServer server)
        {
            using (var connection = new TestConnection(server.Port))
            {
                for (var i = 0; i < 5; i++)
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Transfer-Encoding: chunked",
                        "",
                        "5", "hello",
                        "6", " world",
                        "0",
                         "",
                         "");
                    await ReceiveResponse(connection, server.Context);
                    await Task.Delay(ShortDelay);
                }
            }
        }

        private async Task KeepAliveTimeoutNotTriggeredMidContentLengthRequest(TestServer server)
        {
            using (var connection = new TestConnection(server.Port))
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Content-Length: 8",
                    "",
                    "a");
                await Task.Delay(LongDelay);
                await connection.Send("bcdefgh");
                await ReceiveResponse(connection, server.Context);
            }
        }

        private async Task KeepAliveTimeoutNotTriggeredMidChunkedRequest(TestServer server)
        {
            using (var connection = new TestConnection(server.Port))
            {
                await connection.Send(
                        "POST / HTTP/1.1",
                        "Transfer-Encoding: chunked",
                        "",
                        "5", "hello",
                        "");
                await Task.Delay(LongDelay);
                await connection.Send(
                        "6", " world",
                        "0",
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

        private TestServer CreateServer()
        {
            return new TestServer(App, new TestServiceContext
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

        private async Task App(HttpContext httpContext)
        {
            const string response = "hello, world";
            httpContext.Response.ContentLength = response.Length;
            await httpContext.Response.WriteAsync(response);
        }

        private async Task ReceiveResponse(TestConnection connection, TestServiceContext testServiceContext)
        {
            await connection.Receive(
                "HTTP/1.1 200 OK",
                $"Date: {testServiceContext.DateHeaderValue}",
                "Content-Length: 12",
                "",
                "hello, world");
        }
    }
}
