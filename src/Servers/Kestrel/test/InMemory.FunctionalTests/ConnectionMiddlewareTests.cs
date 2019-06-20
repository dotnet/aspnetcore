// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests
{
    public class ConnectionMiddlewareTests : TestApplicationErrorLoggerLoggedTest
    {
        public static TheoryData<RequestDelegate> EchoAppRequestDelegates =>
            new TheoryData<RequestDelegate>
            {
                { TestApp.EchoApp },
                { TestApp.EchoAppPipeWriter }
            };

        [Theory]
        [MemberData(nameof(EchoAppRequestDelegates))]
        public async Task CanReadAndWriteWithRewritingConnectionAdapter(RequestDelegate requestDelegate)
        {
            RewritingConnectionMiddleware middleware = null;

            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
            listenOptions.Use(next =>
            {
                middleware = new RewritingConnectionMiddleware(next);
                return middleware.OnConnectionAsync;
            });

            var serviceContext = new TestServiceContext(LoggerFactory);

            var sendString = "POST / HTTP/1.0\r\nContent-Length: 12\r\n\r\nHello World?";

            await using (var server = new TestServer(requestDelegate, serviceContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    // "?" changes to "!"
                    await connection.Send(sendString);
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {serviceContext.DateHeaderValue}",
                        "",
                        "Hello World!");
                }
            }

            Assert.Equal(sendString.Length, middleware.BytesRead);
        }

        [Theory]
        [MemberData(nameof(EchoAppRequestDelegates))]
        public async Task CanReadAndWriteWithAsyncConnectionMiddleware(RequestDelegate requestDelegate)
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
            listenOptions.Use(next => new AsyncConnectionMiddleware(next).OnConnectionAsync);

            var serviceContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(requestDelegate, serviceContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.0",
                        "Content-Length: 12",
                        "",
                        "Hello World?");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {serviceContext.DateHeaderValue}",
                        "",
                        "Hello World!");
                }
            }
        }

        [Theory]
        [MemberData(nameof(EchoAppRequestDelegates))]
        public async Task ImmediateFinAfterOnConnectionAsyncClosesGracefully(RequestDelegate requestDelegate)
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
            listenOptions.Use(next => new AsyncConnectionMiddleware(next).OnConnectionAsync);

            var serviceContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(requestDelegate, serviceContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    // FIN
                    connection.ShutdownSend();
                    await connection.WaitForConnectionClose();
                }
            }
        }

        [Theory]
        [MemberData(nameof(EchoAppRequestDelegates))]
        public async Task ImmediateFinAfterThrowingClosesGracefully(RequestDelegate requestDelegate)
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
            listenOptions.Use(next => context => throw new InvalidOperationException());

            var serviceContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(requestDelegate, serviceContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    // FIN
                    connection.ShutdownSend();
                    await connection.WaitForConnectionClose();
                }
            }
        }

        [Theory]
        [CollectDump]
        [MemberData(nameof(EchoAppRequestDelegates))]
        public async Task ImmediateShutdownAfterOnConnectionAsyncDoesNotCrash(RequestDelegate requestDelegate)
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
            listenOptions.Use(next => new AsyncConnectionMiddleware(next).OnConnectionAsync);

            var serviceContext = new TestServiceContext(LoggerFactory);

            TestApplicationErrorLogger.ThrowOnUngracefulShutdown = false;

            var stopTask = Task.CompletedTask;
            await using (var server = new TestServer(requestDelegate, serviceContext, listenOptions))
            using (var shutdownCts = new CancellationTokenSource(TestConstants.DefaultTimeout))
            {
                using (var connection = server.CreateConnection())
                {
                    // We assume all CI servers are really slow, so we use a 30 second default test timeout
                    // instead of the 5 second default production timeout. If this test is still flaky,
                    // *then* we can consider collecting and investigating memory dumps.
                    stopTask = server.StopAsync(shutdownCts.Token);
                }

                await stopTask;
            }
        }

        [Fact]
        public async Task ImmediateShutdownDuringOnConnectionAsyncDoesNotCrash()
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
            listenOptions.Use(next =>
            {
                return async context =>
                {
                    await tcs.Task;
                    await next(context);
                };
            });

            var serviceContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(TestApp.EchoApp, serviceContext, listenOptions))
            {
                Task stopTask;

                using (var connection = server.CreateConnection())
                {
                    stopTask = server.StopAsync();

                    tcs.TrySetResult(null);
                }

                await stopTask;
            }
        }

        [Theory]
        [MemberData(nameof(EchoAppRequestDelegates))]
        public async Task ThrowingSynchronousConnectionMiddlewareDoesNotCrashServer(RequestDelegate requestDelegate)
        {
            var connectionId = "";
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
            listenOptions.Use(next => context =>
            {
                connectionId = context.ConnectionId;
                throw new InvalidOperationException();
            });

            var serviceContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(requestDelegate, serviceContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                       "POST / HTTP/1.0",
                       "Content-Length: 1000",
                       "\r\n");

                    await connection.WaitForConnectionClose();
                }
            }

            Assert.Contains(TestApplicationErrorLogger.Messages, m => m.Message.Contains("Unhandled exception while processing " + connectionId + "."));
        }

        [Fact]
        public async Task CanFlushAsyncWithConnectionMiddleware()
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
                .UsePassThrough();

            var serviceContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async context =>
            {
                await context.Response.WriteAsync("Hello ");
                await context.Response.Body.FlushAsync();
                await context.Response.WriteAsync("World!");
            }, serviceContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.0",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {serviceContext.DateHeaderValue}",
                        "",
                        "Hello World!");
                }
            }
        }

        [Fact]
        public async Task CanFlushAsyncWithConnectionMiddlewarePipeWriter()
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
                .UsePassThrough();

            var serviceContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async context =>
            {
                await context.Response.BodyWriter.WriteAsync(Encoding.ASCII.GetBytes("Hello "));
                await context.Response.BodyWriter.FlushAsync();
                await context.Response.BodyWriter.WriteAsync(Encoding.ASCII.GetBytes("World!"));
            }, serviceContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.0",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {serviceContext.DateHeaderValue}",
                        "",
                        "Hello World!");
                }
            }
        }

        private class RewritingConnectionMiddleware
        {
            private RewritingStream _rewritingStream;
            private readonly ConnectionDelegate _next;

            public RewritingConnectionMiddleware(ConnectionDelegate next)
            {
                _next = next;
            }

            public async Task OnConnectionAsync(ConnectionContext context)
            {
                var old = context.Transport;
                var duplexPipe = new DuplexPipeStreamAdapter<RewritingStream>(context.Transport, s => new RewritingStream(s));
                _rewritingStream = duplexPipe.Stream;

                try
                {
                    await using (duplexPipe)
                    {
                        context.Transport = duplexPipe;
                        await _next(context);
                    }
                }
                finally
                {
                    context.Transport = old;
                }
            }

            public int BytesRead => _rewritingStream.BytesRead;
        }

        private class AsyncConnectionMiddleware
        {
            private readonly ConnectionDelegate _next;

            public AsyncConnectionMiddleware(ConnectionDelegate next)
            {
                _next = next;
            }

            public async Task OnConnectionAsync(ConnectionContext context)
            {
                await Task.Yield();

                var old = context.Transport;
                var duplexPipe = new DuplexPipeStreamAdapter<RewritingStream>(context.Transport, s => new RewritingStream(s));

                try
                {
                    await using (duplexPipe)
                    {
                        context.Transport = duplexPipe;
                        await _next(context);
                    }
                }
                finally
                {
                    context.Transport = old;
                }
            }
        }

        private class RewritingStream : Stream
        {
            private readonly Stream _innerStream;

            public RewritingStream(Stream innerStream)
            {
                _innerStream = innerStream;
            }

            public int BytesRead { get; private set; }

            public override bool CanRead => _innerStream.CanRead;

            public override bool CanSeek => _innerStream.CanSeek;

            public override bool CanWrite => _innerStream.CanWrite;

            public override long Length => _innerStream.Length;

            public override long Position
            {
                get
                {
                    return _innerStream.Position;
                }
                set
                {
                    _innerStream.Position = value;
                }
            }

            public override void Flush()
            {
                _innerStream.Flush();
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return _innerStream.FlushAsync(cancellationToken);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                var actual = _innerStream.Read(buffer, offset, count);

                BytesRead += actual;

                return actual;
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                var actual = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);

                BytesRead += actual;

                return actual;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _innerStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _innerStream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i] == '?')
                    {
                        buffer[i] = (byte)'!';
                    }
                }

                _innerStream.Write(buffer, offset, count);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i] == '?')
                    {
                        buffer[i] = (byte)'!';
                    }
                }

                return _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
            }
        }
    }
}
