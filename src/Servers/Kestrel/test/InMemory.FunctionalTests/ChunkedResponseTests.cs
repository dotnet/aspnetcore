// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests
{
    public class ChunkedResponseTests : LoggedTest
    {
        [Fact]
        public async Task ResponsesAreChunkedAutomatically()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello "), 0, 6));
                await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("World!"), 0, 6));
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
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

        [Fact]
        public async Task ResponsesAreNotChunkedAutomaticallyForHttp10Requests()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Response.WriteAsync("Hello ");
                await httpContext.Response.WriteAsync("World!");
            }, testContext))
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

        [Fact]
        public async Task IgnoresChangesToHttpProtocol()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                httpContext.Request.Protocol = "HTTP/2"; // Doesn't support chunking. This change should be ignored.
                var response = httpContext.Response;
                await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello "), 0, 6));
                await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("World!"), 0, 6));
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
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

        [Fact]
        public async Task ResponsesAreChunkedAutomaticallyForHttp11NonKeepAliveRequests()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Response.WriteAsync("Hello ");
                await httpContext.Response.WriteAsync("World!");
            }, testContext))
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

        [Fact]
        public async Task ResponsesAreChunkedAutomaticallyLargeResponseWithOverloadedWriteAsync()
        {
            var testContext = new TestServiceContext(LoggerFactory);
            var expectedString = new string('a', 10000);
            await using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Response.WriteAsync(expectedString);
            }, testContext))
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
                        "f92",
                        new string('a', 3986),
                        "ff9",
                        new string('a', 4089),
                        "785",
                        new string('a', 1925),
                        "0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [InlineData(4096)]
        [InlineData(10000)]
        [InlineData(100000)]
        public async Task ResponsesAreChunkedAutomaticallyLargeChunksLargeResponseWithOverloadedWriteAsync(int length)
        {
            var testContext = new TestServiceContext(LoggerFactory);
            var expectedString = new string('a', length);
            await using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Response.StartAsync();
                var memory = httpContext.Response.BodyWriter.GetMemory(length);
                Assert.True(length <= memory.Length);
                Encoding.ASCII.GetBytes(expectedString).CopyTo(memory);
                httpContext.Response.BodyWriter.Advance(length);
                await httpContext.Response.BodyWriter.FlushAsync();
            }, testContext))
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
                        length.ToString("x"), 
                        new string('a', length),
                        "0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(16)]
        [InlineData(256)]
        [InlineData(4096)]
        public async Task ResponsesAreChunkedAutomaticallyPartialWrite(int partialLength)
        {
            var testContext = new TestServiceContext(LoggerFactory);
            var expectedString = new string('a', partialLength);
            await using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Response.StartAsync();
                var memory = httpContext.Response.BodyWriter.GetMemory(100000);
                Encoding.ASCII.GetBytes(expectedString).CopyTo(memory);
                httpContext.Response.BodyWriter.Advance(partialLength);
                await httpContext.Response.BodyWriter.FlushAsync();
            }, testContext))
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
                        partialLength.ToString("x"),
                        new string('a', partialLength),
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task SettingConnectionCloseHeaderInAppDoesNotDisableChunking()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.Headers["Connection"] = "close";
                await httpContext.Response.WriteAsync("Hello ");
                await httpContext.Response.WriteAsync("World!");
            }, testContext))
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

        [Fact]
        public async Task ZeroLengthWritesAreIgnored()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello "), 0, 6));
                await response.BodyWriter.WriteAsync(new Memory<byte>(new byte[0], 0, 0));
                await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("World!"), 0, 6));
            }, testContext))
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
                        "6",
                        "World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ZeroLengthWritesFlushHeaders()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            var flushed = new SemaphoreSlim(0, 1);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.WriteAsync("");

                await flushed.WaitAsync();

                await response.WriteAsync("Hello World!");
            }, testContext))
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

                    await connection.Receive(
                        "c",
                        "Hello World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task EmptyResponseBodyHandledCorrectlyWithZeroLengthWrite()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.BodyWriter.WriteAsync(new Memory<byte>(new byte[0], 0, 0));
            }, testContext))
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
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ConnectionClosedIfExceptionThrownAfterWrite()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello World!"), 0, 12));
                throw new Exception();
            }, testContext))
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
                    await connection.ReceiveEnd(
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

        [Fact]
        public async Task ConnectionClosedIfExceptionThrownAfterZeroLengthWrite()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.BodyWriter.WriteAsync(new Memory<byte>(new byte[0], 0, 0));
                throw new Exception();
            }, testContext))
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
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task WritesAreFlushedPriorToResponseCompletion()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            var flushWh = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello "), 0, 6));

                // Don't complete response until client has received the first chunk.
                await flushWh.Task.DefaultTimeout();

                await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("World!"), 0, 6));
            }, testContext))
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

                    flushWh.SetResult(null);

                    await connection.Receive(
                        "6",
                        "World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ChunksCanBeWrittenManually()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                response.Headers["Transfer-Encoding"] = "chunked";

                await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("6\r\nHello \r\n"), 0, 11));
                await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("6\r\nWorld!\r\n"), 0, 11));
                await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("0\r\n\r\n"), 0, 5));
            }, testContext))
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
                        "6",
                        "World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ChunksWithGetMemoryAfterStartAsyncBeforeFirstFlushStillFlushes()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;

                await response.StartAsync();
                var memory = response.BodyWriter.GetMemory();
                var fisrtPartOfResponse = Encoding.ASCII.GetBytes("Hello ");
                fisrtPartOfResponse.CopyTo(memory);
                response.BodyWriter.Advance(6);

                memory = response.BodyWriter.GetMemory();
                var secondPartOfResponse = Encoding.ASCII.GetBytes("World!");
                secondPartOfResponse.CopyTo(memory);
                response.BodyWriter.Advance(6);

                await response.BodyWriter.FlushAsync();
            }, testContext))
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
                        "c",
                        "Hello World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ChunksWithGetMemoryBeforeFirstFlushStillFlushes()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;

                var memory = response.BodyWriter.GetMemory();
                var fisrtPartOfResponse = Encoding.ASCII.GetBytes("Hello ");
                fisrtPartOfResponse.CopyTo(memory);
                response.BodyWriter.Advance(6);

                memory = response.BodyWriter.GetMemory();
                var secondPartOfResponse = Encoding.ASCII.GetBytes("World!");
                secondPartOfResponse.CopyTo(memory);
                response.BodyWriter.Advance(6);

                await response.BodyWriter.FlushAsync();
            }, testContext))
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
                        "c",
                        "Hello World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ChunksWithGetMemoryLargeWriteBeforeFirstFlush()
        {
            var length = new IntAsRef();
            var semaphore = new SemaphoreSlim(initialCount: 0);
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;

                var memory = response.BodyWriter.GetMemory(5000);
                length.Value = memory.Length;
                semaphore.Release();

                var fisrtPartOfResponse = Encoding.ASCII.GetBytes(new string('a', memory.Length));
                fisrtPartOfResponse.CopyTo(memory);
                response.BodyWriter.Advance(memory.Length);

                memory = response.BodyWriter.GetMemory();
                var secondPartOfResponse = Encoding.ASCII.GetBytes("World!");
                secondPartOfResponse.CopyTo(memory);
                response.BodyWriter.Advance(6);

                await response.BodyWriter.FlushAsync();
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host: ",
                        "",
                        "");

                    // Wait for length to be set
                    await semaphore.WaitAsync();

                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        length.Value.ToString("x"),
                        new string('a', length.Value),
                        "6",
                        "World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ChunksWithGetMemoryAndStartAsyncWithInitialFlushWorks()
        {
            var length = new IntAsRef();
            var semaphore = new SemaphoreSlim(initialCount: 0);
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;

                await response.BodyWriter.FlushAsync();

                var memory = response.BodyWriter.GetMemory(5000);
                length.Value = memory.Length;
                semaphore.Release();

                var fisrtPartOfResponse = Encoding.ASCII.GetBytes(new string('a', memory.Length));
                fisrtPartOfResponse.CopyTo(memory);
                response.BodyWriter.Advance(memory.Length);

                memory = response.BodyWriter.GetMemory();
                var secondPartOfResponse = Encoding.ASCII.GetBytes("World!");
                secondPartOfResponse.CopyTo(memory);
                response.BodyWriter.Advance(6);

                await response.BodyWriter.FlushAsync();
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host: ",
                        "",
                        "");

                    // Wait for length to be set
                    await semaphore.WaitAsync();

                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        length.Value.ToString("x"),
                        new string('a', length.Value),
                        "6",
                        "World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ChunksWithGetMemoryBeforeFlushEdgeCase()
        {
            var length = 0;
            var semaphore = new SemaphoreSlim(initialCount: 0);
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;

                await response.StartAsync();

                var memory = response.BodyWriter.GetMemory();
                length = memory.Length - 1;
                semaphore.Release();

                var fisrtPartOfResponse = Encoding.ASCII.GetBytes(new string('a', length));
                fisrtPartOfResponse.CopyTo(memory);
                response.BodyWriter.Advance(length);

                var secondMemory = response.BodyWriter.GetMemory(6);

                var secondPartOfResponse = Encoding.ASCII.GetBytes("World!");
                secondPartOfResponse.CopyTo(secondMemory);
                response.BodyWriter.Advance(6);

                await response.BodyWriter.FlushAsync();
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host: ",
                        "",
                        "");

                    // Wait for length to be set
                    await semaphore.WaitAsync();

                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        length.ToString("x"),
                        new string('a', length),
                        "6",
                        "World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ChunkGetMemoryMultipleAdvance()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;

                await response.StartAsync();

                var memory = response.BodyWriter.GetMemory(4096);
                var fisrtPartOfResponse = Encoding.ASCII.GetBytes("Hello ");
                fisrtPartOfResponse.CopyTo(memory);
                response.BodyWriter.Advance(6);

                var secondPartOfResponse = Encoding.ASCII.GetBytes("World!");
                secondPartOfResponse.CopyTo(memory.Slice(6));
                response.BodyWriter.Advance(6);
            }, testContext))
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
                        "c",
                        "Hello World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ChunkGetSpanMultipleAdvance()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.StartAsync();

                // To avoid using span in an async method
                void NonAsyncMethod()
                {
                    var span = response.BodyWriter.GetSpan(4096);
                    var fisrtPartOfResponse = Encoding.ASCII.GetBytes("Hello ");
                    fisrtPartOfResponse.CopyTo(span);
                    response.BodyWriter.Advance(6);

                    var secondPartOfResponse = Encoding.ASCII.GetBytes("World!");
                    secondPartOfResponse.CopyTo(span.Slice(6));
                    response.BodyWriter.Advance(6);
                }

                NonAsyncMethod();
            }, testContext))
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
                        "c",
                        "Hello World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ChunkGetMemoryAndWrite()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;

                await response.StartAsync();

                var memory = response.BodyWriter.GetMemory(4096);

                var fisrtPartOfResponse = Encoding.ASCII.GetBytes("Hello ");
                fisrtPartOfResponse.CopyTo(memory);
                response.BodyWriter.Advance(6);

                await response.WriteAsync("World!");
            }, testContext))
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
                        "c",
                        "Hello World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ChunkGetMemoryAndWriteWithoutStart()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;

                var memory = response.BodyWriter.GetMemory(4096);
                var fisrtPartOfResponse = Encoding.ASCII.GetBytes("Hello ");
                fisrtPartOfResponse.CopyTo(memory);
                response.BodyWriter.Advance(6);

                await response.WriteAsync("World!");
            }, testContext))
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
                        "6",
                        "World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task GetMemoryWithSizeHint()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;

                await response.StartAsync();

                var memory = response.BodyWriter.GetMemory(0);

                // Headers are already written to memory, sliced appropriately
                Assert.Equal(4005, memory.Length);

                memory = response.BodyWriter.GetMemory(1000000);
                Assert.Equal(4005, memory.Length);
            }, testContext))
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
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task GetMemoryWithSizeHintWithoutStartAsync()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;

                var memory = response.BodyWriter.GetMemory(0);

                Assert.Equal(4096, memory.Length);

                memory = response.BodyWriter.GetMemory(1000000);
                Assert.Equal(1000000, memory.Length);
                await Task.CompletedTask;
            }, testContext))
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
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [InlineData(15)]
        [InlineData(255)]
        public async Task ChunkGetMemoryWithoutStartWithSmallerSizesWork(int writeSize)
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                var memory = response.BodyWriter.GetMemory(4096);
                var fisrtPartOfResponse = Encoding.ASCII.GetBytes(new string('a', writeSize));
                fisrtPartOfResponse.CopyTo(memory);
                response.BodyWriter.Advance(writeSize);
                await response.BodyWriter.FlushAsync();
            }, testContext))
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
                        writeSize.ToString("X").ToLower(),
                        new string('a', writeSize),
                        "0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [InlineData(15)]
        [InlineData(255)]
        public async Task ChunkGetMemoryWithStartWithSmallerSizesWork(int writeSize)
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;

                var memory = response.BodyWriter.GetMemory(4096);
                var fisrtPartOfResponse = Encoding.ASCII.GetBytes(new string('a', writeSize));
                fisrtPartOfResponse.CopyTo(memory);
                response.BodyWriter.Advance(writeSize);
                await response.BodyWriter.FlushAsync();
            }, testContext))
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
                        writeSize.ToString("X").ToLower(),
                        new string('a', writeSize),
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ChunkedWithBothPipeAndStreamWorks()
        {
            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;

                var memory = response.BodyWriter.GetMemory(4096);
                var fisrtPartOfResponse = Encoding.ASCII.GetBytes("hello,");
                fisrtPartOfResponse.CopyTo(memory);
                response.BodyWriter.Advance(6);
                var secondPartOfResponse = Encoding.ASCII.GetBytes(" world");
                secondPartOfResponse.CopyTo(memory.Slice(6));
                response.BodyWriter.Advance(6);

                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("hello, world"));
                await response.BodyWriter.WriteAsync(Encoding.ASCII.GetBytes("hello, world"));
                await response.WriteAsync("hello, world");

            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "c",
                        "hello, world",
                        "c",
                        "hello, world",
                        "c",
                        "hello, world",
                        "c",
                        "hello, world",
                        "0",
                        "",
                        "");
                }
            }
        }

        private class IntAsRef
        {
            public int Value { get; set; }
        }
    }
}

