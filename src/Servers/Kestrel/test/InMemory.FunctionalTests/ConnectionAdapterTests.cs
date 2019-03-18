// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests
{
    public class ConnectionAdapterTests : TestApplicationErrorLoggerLoggedTest
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
            var adapter = new RewritingConnectionAdapter();
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { adapter }
            };

            var serviceContext = new TestServiceContext(LoggerFactory);

            var sendString = "POST / HTTP/1.0\r\nContent-Length: 12\r\n\r\nHello World?";

            using (var server = new TestServer(requestDelegate, serviceContext, listenOptions))
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
                await server.StopAsync();
            }

            Assert.Equal(sendString.Length, adapter.BytesRead);
        }

        [Theory]
        [MemberData(nameof(EchoAppRequestDelegates))]
        public async Task CanReadAndWriteWithAsyncConnectionAdapter(RequestDelegate requestDelegate)
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { new AsyncConnectionAdapter() }
            };

            var serviceContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(requestDelegate, serviceContext, listenOptions))
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
                await server.StopAsync();
            }
        }

        [Theory]
        [MemberData(nameof(EchoAppRequestDelegates))]
        public async Task ImmediateFinAfterOnConnectionAsyncClosesGracefully(RequestDelegate requestDelegate)
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { new AsyncConnectionAdapter() }
            };

            var serviceContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(requestDelegate, serviceContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    // FIN
                    connection.ShutdownSend();
                    await connection.WaitForConnectionClose();
                }
                await server.StopAsync();
            }
        }

        [Theory]
        [MemberData(nameof(EchoAppRequestDelegates))]
        public async Task ImmediateFinAfterThrowingClosesGracefully(RequestDelegate requestDelegate)
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { new ThrowingConnectionAdapter() }
            };

            var serviceContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(requestDelegate, serviceContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    // FIN
                    connection.ShutdownSend();
                    await connection.WaitForConnectionClose();
                }
                await server.StopAsync();
            }
        }

        [Theory]
        [CollectDump]
        [MemberData(nameof(EchoAppRequestDelegates))]
        public async Task ImmediateShutdownAfterOnConnectionAsyncDoesNotCrash(RequestDelegate requestDelegate)
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { new AsyncConnectionAdapter() }
            };

            var serviceContext = new TestServiceContext(LoggerFactory);

            var stopTask = Task.CompletedTask;
            using (var server = new TestServer(requestDelegate, serviceContext, listenOptions))
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
            var waitingConnectionAdapter = new WaitingConnectionAdapter();
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { waitingConnectionAdapter }
            };

            var serviceContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(TestApp.EchoApp, serviceContext, listenOptions))
            {
                Task stopTask;

                using (var connection = server.CreateConnection())
                {
                    var closingMessageTask = TestApplicationErrorLogger.WaitForMessage(m => m.Message.Contains(CoreStrings.ServerShutdownDuringConnectionInitialization));

                    stopTask = server.StopAsync();

                    await closingMessageTask.DefaultTimeout();

                    waitingConnectionAdapter.Complete();
                }

                await stopTask;
            }
        }

        [Theory]
        [MemberData(nameof(EchoAppRequestDelegates))]
        public async Task ThrowingSynchronousConnectionAdapterDoesNotCrashServer(RequestDelegate requestDelegate)
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { new ThrowingConnectionAdapter() }
            };

            var serviceContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(requestDelegate, serviceContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                       "POST / HTTP/1.0",
                       "Content-Length: 1000",
                       "\r\n");

                    await connection.WaitForConnectionClose();
                }
                await server.StopAsync();
            }

            Assert.Contains(TestApplicationErrorLogger.Messages, m => m.Message.Contains($"Uncaught exception from the {nameof(IConnectionAdapter.OnConnectionAsync)} method of an {nameof(IConnectionAdapter)}."));
        }

        [Fact]
        public async Task CanFlushAsyncWithConnectionAdapter()
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { new PassThroughConnectionAdapter() }
            };

            var serviceContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async context =>
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
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task CanFlushAsyncWithConnectionAdapterPipeWriter()
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { new PassThroughConnectionAdapter() }
            };

            var serviceContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async context =>
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
                await server.StopAsync();
            }
        }

        private class RewritingConnectionAdapter : IConnectionAdapter
        {
            private RewritingStream _rewritingStream;

            public bool IsHttps => false;

            public Task<IAdaptedConnection> OnConnectionAsync(ConnectionAdapterContext context)
            {
                _rewritingStream = new RewritingStream(context.ConnectionStream);
                return Task.FromResult<IAdaptedConnection>(new AdaptedConnection(_rewritingStream));
            }

            public int BytesRead => _rewritingStream.BytesRead;
        }

        private class AsyncConnectionAdapter : IConnectionAdapter
        {
            public bool IsHttps => false;

            public async Task<IAdaptedConnection> OnConnectionAsync(ConnectionAdapterContext context)
            {
                await Task.Yield();
                return new AdaptedConnection(new RewritingStream(context.ConnectionStream));
            }
        }

        private class WaitingConnectionAdapter : IConnectionAdapter
        {
            private TaskCompletionSource<object> _waitingTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            public bool IsHttps => false;

            public async Task<IAdaptedConnection> OnConnectionAsync(ConnectionAdapterContext context)
            {
                await _waitingTcs.Task;
                return new AdaptedConnection(context.ConnectionStream);
            }

            public void Complete()
            {
                _waitingTcs.TrySetResult(null);
            }
        }

        private class ThrowingConnectionAdapter : IConnectionAdapter
        {
            public bool IsHttps => false;

            public Task<IAdaptedConnection> OnConnectionAsync(ConnectionAdapterContext context)
            {
                throw new Exception();
            }
        }

        private class AdaptedConnection : IAdaptedConnection
        {
            public AdaptedConnection(Stream adaptedStream)
            {
                ConnectionStream = adaptedStream;
            }

            public Stream ConnectionStream { get; }

            public void Dispose()
            {
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
                var actual = await _innerStream.ReadAsync(buffer, offset, count);

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
