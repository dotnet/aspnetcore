// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class RequestHeadersTimeoutTests
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
                    ConnectionAbortedWhenRequestHeadersNotReceivedInTime(server, ""),
                    ConnectionAbortedWhenRequestHeadersNotReceivedInTime(server, "Content-Length: 1\r\n"),
                    ConnectionAbortedWhenRequestHeadersNotReceivedInTime(server, "Content-Length: 1\r\n\r"),
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
                await ReceiveTimeoutResponse(connection, server.Context);
            }
        }

        private async Task RequestHeadersTimeoutCanceledAfterHeadersReceived(TestServer server)
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Content-Length: 1",
                    "",
                    "");
                await Task.Delay(RequestHeadersTimeout);
                await connection.Send(
                    "a");
                await ReceiveResponse(connection, server.Context);
            }
        }

        private async Task ConnectionAbortedWhenRequestLineNotReceivedInTime(TestServer server, string requestLine)
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(requestLine);
                await ReceiveTimeoutResponse(connection, server.Context);
            }
        }

        private async Task TimeoutNotResetOnEachRequestLineCharacterReceived(TestServer server)
        {
            using (var connection = server.CreateConnection())
            {
                await Assert.ThrowsAsync<IOException>(async () =>
                {
                    foreach (var ch in "POST / HTTP/1.1\r\n\r\n")
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
                new TestServiceContext
                {
                    ServerOptions = new KestrelServerOptions
                    {
                        AddServerHeader = false,
                        Limits =
                        {
                            RequestHeadersTimeout = RequestHeadersTimeout
                        }
                    }
                });
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

        private async Task ReceiveTimeoutResponse(TestConnection connection, TestServiceContext testServiceContext)
        {
            await connection.ReceiveForcedEnd(
                "HTTP/1.1 408 Request Timeout",
                "Connection: close",
                $"Date: {testServiceContext.DateHeaderValue}",
                "Content-Length: 0",
                "",
                "");
        }
    }
}