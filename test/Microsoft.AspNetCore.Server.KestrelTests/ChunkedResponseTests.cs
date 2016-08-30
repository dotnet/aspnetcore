// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class ChunkedResponseTests
    {
        public static TheoryData<TestServiceContext> ConnectionFilterData
        {
            get
            {
                return new TheoryData<TestServiceContext>
                {
                    {
                        new TestServiceContext()
                    },
                    {
                        new TestServiceContext(new PassThroughConnectionFilter())
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ResponsesAreChunkedAutomatically(TestServiceContext testContext)
        {
            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello "), 0, 6);
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("World!"), 0, 6);
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.1",
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
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ResponsesAreNotChunkedAutomaticallyForHttp10RequestsAndHttp11NonKeepAliveRequests(TestServiceContext testContext)
        {
            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello "), 0, 6);
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("World!"), 0, 6);
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendEnd(
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

                using (var connection = server.CreateConnection())
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.1",
                        "Connection: close",
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
        [MemberData(nameof(ConnectionFilterData))]
        public async Task SettingConnectionCloseHeaderInAppDisablesChunking(TestServiceContext testContext)
        {
            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                response.Headers["Connection"] = "close";
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello "), 0, 6);
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("World!"), 0, 6);
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.1",
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
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ZeroLengthWritesAreIgnored(TestServiceContext testContext)
        {
            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello "), 0, 6);
                await response.Body.WriteAsync(new byte[0], 0, 0);
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("World!"), 0, 6);
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.1",
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
        [MemberData(nameof(ConnectionFilterData))]
        public async Task EmptyResponseBodyHandledCorrectlyWithZeroLengthWrite(TestServiceContext testContext)
        {
            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.Body.WriteAsync(new byte[0], 0, 0);
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.1",
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
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ConnectionClosedIfExeptionThrownAfterWrite(TestServiceContext testContext)
        {
            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World!"), 0, 12);
                throw new Exception();
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    // SendEnd is not called, so it isn't the client closing the connection.
                    // client closing the connection.
                    await connection.Send(
                        "GET / HTTP/1.1",
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
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ConnectionClosedIfExeptionThrownAfterZeroLengthWrite(TestServiceContext testContext)
        {
            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.Body.WriteAsync(new byte[0], 0, 0);
                throw new Exception();
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    // SendEnd is not called, so it isn't the client closing the connection.
                    await connection.Send(
                        "GET / HTTP/1.1",
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
        [MemberData(nameof(ConnectionFilterData))]
        public async Task WritesAreFlushedPriorToResponseCompletion(TestServiceContext testContext)
        {
            var flushWh = new ManualResetEventSlim();

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello "), 0, 6);

                // Don't complete response until client has received the first chunk.
                flushWh.Wait();

                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("World!"), 0, 6);
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.1",
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
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ChunksCanBeWrittenManually(TestServiceContext testContext)
        {
            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                response.Headers["Transfer-Encoding"] = "chunked";

                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("6\r\nHello \r\n"), 0, 11);
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("6\r\nWorld!\r\n"), 0, 11);
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("0\r\n\r\n"), 0, 5);
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.1",
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

