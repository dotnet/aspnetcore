// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Filter;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class ConnectionFilterTests
    {
        private async Task App(HttpContext httpContext)
        {
            var request = httpContext.Request;
            var response = httpContext.Response;
            while (true)
            {
                var buffer = new byte[8192];
                var count = await request.Body.ReadAsync(buffer, 0, buffer.Length);
                if (count == 0)
                {
                    break;
                }
                await response.Body.WriteAsync(buffer, 0, count);
            }
        }

        [Fact]
        public async Task CanReadAndWriteWithRewritingConnectionFilter()
        {
            var filter = new RewritingConnectionFilter();
            var serviceContext = new TestServiceContext(filter);

            var sendString = "POST / HTTP/1.0\r\n\r\nHello World?";

            using (var server = new TestServer(App, serviceContext))
            {
                using (var connection = server.CreateConnection())
                {
                    // "?" changes to "!"
                    await connection.SendEnd(sendString);
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

            using (var server = new TestServer(App, serviceContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendEnd(
                        "POST / HTTP/1.0",
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

            using (var server = new TestServer(App, serviceContext))
            {
                using (var connection = server.CreateConnection())
                {
                    try
                    {
                        await connection.SendEnd(
                            "POST / HTTP/1.0",
                            "",
                            "Hello World?");
                    }
                    catch (IOException)
                    {
                        // Will throw because the exception in the connection filter will close the connection.
                        Assert.True(true);
                    }
                }
            }
        }

        private class RewritingConnectionFilter : IConnectionFilter
        {
            private static Task _empty = Task.FromResult<object>(null);

            private RewritingStream _rewritingStream;

            public Task OnConnectionAsync(ConnectionFilterContext context)
            {
                _rewritingStream = new RewritingStream(context.Connection);
                context.Connection = _rewritingStream;
                return _empty;
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
        }
    }
}
