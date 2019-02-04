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

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.BodyPipe.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello "), 0, 6));
                await response.BodyPipe.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("World!"), 0, 6));
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
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task ResponsesAreNotChunkedAutomaticallyForHttp10Requests()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
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
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task ResponsesAreChunkedAutomaticallyForHttp11NonKeepAliveRequests()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
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
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task SettingConnectionCloseHeaderInAppDoesNotDisableChunking()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
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
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task ZeroLengthWritesAreIgnored()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.BodyPipe.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello "), 0, 6));
                await response.BodyPipe.WriteAsync(new Memory<byte>(new byte[0], 0, 0));
                await response.BodyPipe.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("World!"), 0, 6));
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
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task ZeroLengthWritesFlushHeaders()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            var flushed = new SemaphoreSlim(0, 1);

            using (var server = new TestServer(async httpContext =>
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
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task EmptyResponseBodyHandledCorrectlyWithZeroLengthWrite()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.BodyPipe.WriteAsync(new Memory<byte>(new byte[0], 0, 0));
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
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task ConnectionClosedIfExceptionThrownAfterWrite()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.BodyPipe.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello World!"), 0, 12));
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
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task ConnectionClosedIfExceptionThrownAfterZeroLengthWrite()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.BodyPipe.WriteAsync(new Memory<byte>(new byte[0], 0, 0));
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
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task WritesAreFlushedPriorToResponseCompletion()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            var flushWh = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.BodyPipe.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello "), 0, 6));

                // Don't complete response until client has received the first chunk.
                await flushWh.Task.DefaultTimeout();

                await response.BodyPipe.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("World!"), 0, 6));
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
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task ChunksCanBeWrittenManually()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                response.Headers["Transfer-Encoding"] = "chunked";

                await response.BodyPipe.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("6\r\nHello \r\n"), 0, 11));
                await response.BodyPipe.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("6\r\nWorld!\r\n"), 0, 11));
                await response.BodyPipe.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("0\r\n\r\n"), 0, 5));
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

                await server.StopAsync();
            }
        }

        [Fact]
        public async Task ChunksWithGetMemoryBeforeFirstFlushStillFlushes()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.StartAsync();
                var memory = response.BodyPipe.GetMemory();
                var fisrtPartOfResponse = Encoding.ASCII.GetBytes("Hello ");
                fisrtPartOfResponse.CopyTo(memory);
                response.BodyPipe.Advance(6);

                memory = response.BodyPipe.GetMemory();
                var secondPartOfResponse = Encoding.ASCII.GetBytes("World!");
                secondPartOfResponse.CopyTo(memory);
                response.BodyPipe.Advance(6);

                await response.BodyPipe.FlushAsync();
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

                await server.StopAsync();
            }
        }

        [Fact]
        public async Task ChunksWithGetMemoryLargeWriteBeforeFirstFlush()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.StartAsync();

                var memory = response.BodyPipe.GetMemory(5000); // This will return 4089
                var fisrtPartOfResponse = Encoding.ASCII.GetBytes(new string('a', memory.Length));
                fisrtPartOfResponse.CopyTo(memory);
                response.BodyPipe.Advance(memory.Length);

                memory = response.BodyPipe.GetMemory();
                var secondPartOfResponse = Encoding.ASCII.GetBytes("World!");
                secondPartOfResponse.CopyTo(memory);
                response.BodyPipe.Advance(6);

                await response.BodyPipe.FlushAsync();
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
                        "ff9",
                        new string('a', 4089),
                        "6",
                        "World!",
                        "0",
                        "",
                        "");
                }

                await server.StopAsync();
            }
        }

        [Fact]
        public async Task ChunksWithGetMemoryWithInitialFlushWorks()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;

                await response.BodyPipe.FlushAsync();

                var memory = response.BodyPipe.GetMemory(5000); // This will return 4089
                var fisrtPartOfResponse = Encoding.ASCII.GetBytes(new string('a', memory.Length));
                fisrtPartOfResponse.CopyTo(memory);
                response.BodyPipe.Advance(memory.Length);

                memory = response.BodyPipe.GetMemory();
                var secondPartOfResponse = Encoding.ASCII.GetBytes("World!");
                secondPartOfResponse.CopyTo(memory);
                response.BodyPipe.Advance(6);

                await response.BodyPipe.FlushAsync();
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
                        "ff9",
                        new string('a', 4089),
                        "6",
                        "World!",
                        "0",
                        "",
                        "");
                }

                await server.StopAsync();
            }
        }

        [Fact]
        public async Task ChunkGetMemoryMultipleAdvance()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;

                await response.StartAsync();

                var memory = response.BodyPipe.GetMemory(4096);
                var fisrtPartOfResponse = Encoding.ASCII.GetBytes("Hello ");
                fisrtPartOfResponse.CopyTo(memory);
                response.BodyPipe.Advance(6);

                var secondPartOfResponse = Encoding.ASCII.GetBytes("World!");
                secondPartOfResponse.CopyTo(memory.Slice(6));
                response.BodyPipe.Advance(6);
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

                await server.StopAsync();
            }
        }

        [Fact]
        public async Task ChunkGetSpanMultipleAdvance()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;

                // To avoid using span in an async method
                void NonAsyncMethod()
                {
                    var span = response.BodyPipe.GetSpan(4096);
                    var fisrtPartOfResponse = Encoding.ASCII.GetBytes("Hello ");
                    fisrtPartOfResponse.CopyTo(span);
                    response.BodyPipe.Advance(6);

                    var secondPartOfResponse = Encoding.ASCII.GetBytes("World!");
                    secondPartOfResponse.CopyTo(span.Slice(6));
                    response.BodyPipe.Advance(6);
                }

                await response.StartAsync();

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

                await server.StopAsync();
            }
        }

        [Fact]
        public async Task ChunkGetMemoryAndWrite()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;

                await response.StartAsync();

                var memory = response.BodyPipe.GetMemory(4096);
                var fisrtPartOfResponse = Encoding.ASCII.GetBytes("Hello ");
                fisrtPartOfResponse.CopyTo(memory);
                response.BodyPipe.Advance(6);

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

                await server.StopAsync();
            }
        }

        [Fact]
        public async Task GetMemoryWithSizeHint()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;

                await response.StartAsync();

                var memory = response.BodyPipe.GetMemory(0);

                // Headers are already written to memory, sliced appropriately
                Assert.Equal(4005, memory.Length);

                memory = response.BodyPipe.GetMemory(1000000);
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

                await server.StopAsync();
            }
        }

        [Theory]
        [InlineData(15)]
        [InlineData(255)]
        public async Task ChunkGetMemoryWithSmallerSizesWork(int writeSize)
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;

                await response.StartAsync();

                var memory = response.BodyPipe.GetMemory(4096);
                var fisrtPartOfResponse = Encoding.ASCII.GetBytes(new string('a', writeSize));
                fisrtPartOfResponse.CopyTo(memory);
                response.BodyPipe.Advance(writeSize);
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

                await server.StopAsync();
            }
        }

        [Fact]
        public async Task ChunkedWithBothPipeAndStreamWorks()
        {
            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                await response.StartAsync();
                var memory = response.BodyPipe.GetMemory(4096);
                var fisrtPartOfResponse = Encoding.ASCII.GetBytes("hello,");
                fisrtPartOfResponse.CopyTo(memory);
                response.BodyPipe.Advance(6);
                var secondPartOfResponse = Encoding.ASCII.GetBytes(" world");
                secondPartOfResponse.CopyTo(memory.Slice(6));
                response.BodyPipe.Advance(6);

                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("hello, world"));
                await response.BodyPipe.WriteAsync(Encoding.ASCII.GetBytes("hello, world"));
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
                await server.StopAsync();
            }
        }
    }
}

