// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Filter;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class ConnectionFilterTests
    {
        [Fact]
        public async Task CanReadAndWriteWithRewritingConnectionFilter()
        {
            var filter = new RewritingConnectionFilter();
            var serviceContext = new TestServiceContext(filter);

            var sendString = "POST / HTTP/1.0\r\nContent-Length: 12\r\n\r\nHello World?";

            using (var server = new TestServer(TestApp.EchoApp, serviceContext))
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

            Assert.Equal(sendString.Length, filter.BytesRead);
        }

        [Fact]
        public async Task CanReadAndWriteWithAsyncConnectionFilter()
        {
            var serviceContext = new TestServiceContext(new AsyncConnectionFilter());

            using (var server = new TestServer(TestApp.EchoApp, serviceContext))
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
        public async Task ThrowingSynchronousConnectionFilterDoesNotCrashServer()
        {
            var serviceContext = new TestServiceContext(new ThrowingConnectionFilter());

            using (var server = new TestServer(TestApp.EchoApp, serviceContext))
            {
                using (var connection = server.CreateConnection())
                {
                    // Will throw because the exception in the connection filter will close the connection.
                    await Assert.ThrowsAsync<IOException>(async () =>
                    {
                        await connection.Send(
                           "POST / HTTP/1.0",
                           "Content-Length: 1000",
                           "\r\n");

                        for (var i = 0; i < 1000; i++)
                        {
                            await connection.Send("a");
                            await Task.Delay(5);
                        }
                    });
                }
            }
        }

        private class RewritingConnectionFilter : IConnectionFilter
        {
            private RewritingStream _rewritingStream;

            public Task OnConnectionAsync(ConnectionFilterContext context)
            {
                _rewritingStream = new RewritingStream(context.Connection);
                context.Connection = _rewritingStream;
                return TaskCache.CompletedTask;
            }

            public int BytesRead => _rewritingStream.BytesRead;
        }

        private class AsyncConnectionFilter : IConnectionFilter
        {
            public async Task OnConnectionAsync(ConnectionFilterContext context)
            {
                var oldConnection = context.Connection;

                // Set Connection to null to ensure it isn't used until the returned task completes.
                context.Connection = null;
                await Task.Delay(100);

                context.Connection = new RewritingStream(oldConnection);
            }
        }

        private class ThrowingConnectionFilter : IConnectionFilter
        {
            public Task OnConnectionAsync(ConnectionFilterContext context)
            {
                throw new Exception();
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
                // No-op
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
