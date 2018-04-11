// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class ChunkedResponseTests : LoggedTest
    {
        public static TheoryData<ListenOptions> ConnectionAdapterData => new TheoryData<ListenOptions>
        {
            new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0)),
            new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { new PassThroughConnectionAdapter() }
            }
        };

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ResponsesAreChunkedAutomatically(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello "), 0, 6);
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("World!"), 0, 6);
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "6",
                        "Hello ",
                        "6",
                        "World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ResponsesAreNotChunkedAutomaticallyForHttp10Requests(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Response.WriteAsync("Hello ");
                await httpContext.Response.WriteAsync("World!");
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.0",
                        "Connection: keep-alive",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "",
                        "Hello World!");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ResponsesAreChunkedAutomaticallyForHttp11NonKeepAliveRequests(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Response.WriteAsync("Hello ");
                await httpContext.Response.WriteAsync("World!");
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host: ",
                        "Connection: close",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "6",
                        "Hello ",
                        "6",
                        "World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task SettingConnectionCloseHeaderInAppDoesNotDisableChunking(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.Headers["Connection"] = "close";
                await httpContext.Response.WriteAsync("Hello ");
                await httpContext.Response.WriteAsync("World!");
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host: ",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "6",
                        "Hello ",
                        "6",
                        "World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ZeroLengthWritesAreIgnored(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello "), 0, 6);
                await response.Body.WriteAsync(new byte[0], 0, 0);
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("World!"), 0, 6);
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host: ",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "6",
                        "Hello ",
                        "6",
                        "World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ZeroLengthWritesFlushHeaders(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext(LoggerFactory);

            var flushed = new SemaphoreSlim(0, 1);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.WriteAsync("");

                await flushed.WaitAsync();

                await response.WriteAsync("Hello World!");
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host: ",
                        "",
                        "");

                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "");

                    flushed.Release();

                    await connection.ReceiveEnd(
                        "c",
                        "Hello World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task EmptyResponseBodyHandledCorrectlyWithZeroLengthWrite(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.Body.WriteAsync(new byte[0], 0, 0);
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host: ",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ConnectionClosedIfExceptionThrownAfterWrite(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World!"), 0, 12);
                throw new Exception();
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    // SendEnd is not called, so it isn't the client closing the connection.
                    // client closing the connection.
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host: ",
                        "",
                        "");
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "c",
                        "Hello World!",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ConnectionClosedIfExceptionThrownAfterZeroLengthWrite(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.Body.WriteAsync(new byte[0], 0, 0);
                throw new Exception();
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    // SendEnd is not called, so it isn't the client closing the connection.
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host: ",
                        "",
                        "");

                    // Headers are sent before connection is closed, but chunked body terminator isn't sent
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task WritesAreFlushedPriorToResponseCompletion(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext(LoggerFactory);

            var flushWh = new ManualResetEventSlim();

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello "), 0, 6);

                // Don't complete response until client has received the first chunk.
                flushWh.Wait();

                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("World!"), 0, 6);
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host: ",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "6",
                        "Hello ",
                        "");

                    flushWh.Set();

                    await connection.ReceiveEnd(
                        "6",
                        "World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ChunksCanBeWrittenManually(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                response.Headers["Transfer-Encoding"] = "chunked";

                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("6\r\nHello \r\n"), 0, 11);
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("6\r\nWorld!\r\n"), 0, 11);
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("0\r\n\r\n"), 0, 5);
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host: ",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "6",
                        "Hello ",
                        "6",
                        "World!",
                        "0",
                        "",
                        "");
                }
            }
        }
    }
}

