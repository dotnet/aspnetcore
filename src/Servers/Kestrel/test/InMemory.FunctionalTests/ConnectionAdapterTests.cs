// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests
{
    public class ConnectionAdapterTests : TestApplicationErrorLoggerLoggedTest
    {
        [Fact]
        public async Task CanReadAndWriteWithRewritingConnectionAdapter()
        {
            var adapter = new RewritingConnectionAdapter();
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { adapter }
            };

            var serviceContext = new TestServiceContext(LoggerFactory);

            var sendString = "POST / HTTP/1.0\r\nContent-Length: 12\r\n\r\nHello World?";

            using (var server = new TestServer(TestApp.EchoApp, serviceContext, listenOptions))
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

            Assert.Equal(sendString.Length, adapter.BytesRead);
        }

        [Fact]
        public async Task CanReadAndWriteWithAsyncConnectionAdapter()
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { new AsyncConnectionAdapter() }
            };

            var serviceContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(TestApp.EchoApp, serviceContext, listenOptions))
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

        [Fact]
        public async Task ImmediateFinAfterOnConnectionAsyncClosesGracefully()
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { new AsyncConnectionAdapter() }
            };

            var serviceContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(TestApp.EchoApp, serviceContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    // FIN
                    connection.ShutdownSend();
                    await connection.WaitForConnectionClose();
                }
            }
        }

        [Fact]
        public async Task ImmediateFinAfterThrowingClosesGracefully()
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { new ThrowingConnectionAdapter() }
            };

            var serviceContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(TestApp.EchoApp, serviceContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    // FIN
                    connection.ShutdownSend();
                    await connection.WaitForConnectionClose();
                }
            }
        }

        [Fact]
        public async Task ImmediateShutdownAfterOnConnectionAsyncDoesNotCrash()
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { new AsyncConnectionAdapter() }
            };

            var serviceContext = new TestServiceContext(LoggerFactory);

            var stopTask = Task.CompletedTask;
            using (var server = new TestServer(TestApp.EchoApp, serviceContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    stopTask = server.StopAsync();
                }

                await stopTask;
            }
        }

        [Fact]
        public async Task ThrowingSynchronousConnectionAdapterDoesNotCrashServer()
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { new ThrowingConnectionAdapter() }
            };

            var serviceContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(TestApp.EchoApp, serviceContext, listenOptions))
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
