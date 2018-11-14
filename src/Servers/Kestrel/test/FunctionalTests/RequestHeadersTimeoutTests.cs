// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class RequestHeadersTimeoutTests : LoggedTest
    {
        private static readonly TimeSpan RequestHeadersTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan LongDelay = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan ShortDelay = TimeSpan.FromSeconds(LongDelay.TotalSeconds / 10);

        [Fact]
        public async Task TestRequestHeadersTimeout()
        {
            using (var server = CreateServer())
            {
                var tasks = new[]
                {
                    ConnectionAbortedWhenRequestHeadersNotReceivedInTime(server, "Host:\r\n"),
                    ConnectionAbortedWhenRequestHeadersNotReceivedInTime(server, "Host:\r\nContent-Length: 1\r\n"),
                    ConnectionAbortedWhenRequestHeadersNotReceivedInTime(server, "Host:\r\nContent-Length: 1\r\n\r"),
                    RequestHeadersTimeoutCanceledAfterHeadersReceived(server),
                    ConnectionAbortedWhenRequestLineNotReceivedInTime(server, "P"),
                    ConnectionAbortedWhenRequestLineNotReceivedInTime(server, "POST / HTTP/1.1\r"),
                    TimeoutNotResetOnEachRequestLineCharacterReceived(server)
                };

                await Task.WhenAll(tasks);
            }
        }

        private async Task ConnectionAbortedWhenRequestHeadersNotReceivedInTime(TestServer server, string headers)
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "GET / HTTP/1.1",
                    headers);
                await ReceiveTimeoutResponse(connection);
            }
        }

        private async Task RequestHeadersTimeoutCanceledAfterHeadersReceived(TestServer server)
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: 1",
                    "",
                    "");
                await Task.Delay(RequestHeadersTimeout);
                await connection.Send(
                    "a");
                await ReceiveResponse(connection);
            }
        }

        private async Task ConnectionAbortedWhenRequestLineNotReceivedInTime(TestServer server, string requestLine)
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(requestLine);
                await ReceiveTimeoutResponse(connection);
            }
        }

        private async Task TimeoutNotResetOnEachRequestLineCharacterReceived(TestServer server)
        {
            using (var connection = server.CreateConnection())
            {
                await Assert.ThrowsAsync<IOException>(async () =>
                {
                    foreach (var ch in "POST / HTTP/1.1\r\nHost:\r\n\r\n")
                    {
                        await connection.Send(ch.ToString());
                        await Task.Delay(ShortDelay);
                    }
                });
            }
        }

        private TestServer CreateServer()
        {
            return new TestServer(async httpContext =>
            {
                await httpContext.Request.Body.ReadAsync(new byte[1], 0, 1);
                await httpContext.Response.WriteAsync("hello, world");
            },
            new TestServiceContext(LoggerFactory)
            {
                // Use real SystemClock so timeouts trigger.
                SystemClock = new SystemClock(),
                ServerOptions =
                {
                    AddServerHeader = false,
                    Limits =
                    {
                        RequestHeadersTimeout = RequestHeadersTimeout,
                        MinRequestBodyDataRate = null
                    }
                }
            });
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

        private async Task ReceiveTimeoutResponse(TestConnection connection)
        {
            await connection.Receive(
                "HTTP/1.1 408 Request Timeout",
                "Connection: close",
                "");
            await connection.ReceiveStartsWith("Date: ");
            await connection.ReceiveForcedEnd(
                "Content-Length: 0",
                "",
                "");
        }
    }
}