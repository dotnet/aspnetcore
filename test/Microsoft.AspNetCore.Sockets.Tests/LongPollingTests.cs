// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Sockets.Features;
using Microsoft.AspNetCore.Sockets.Internal.Transports;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests
{
    public class LongPollingTests
    {
        [Fact]
        public async Task Set204StatusCodeWhenChannelComplete()
        {
            var toApplication = Channel.CreateUnbounded<byte[]>();
            var toTransport = Channel.CreateUnbounded<byte[]>();
            var context = new DefaultHttpContext();
            var connection = new DefaultConnectionContext("foo", toTransport, toApplication);

            var poll = new LongPollingTransport(CancellationToken.None, toTransport.Reader, connectionId: string.Empty, loggerFactory: new LoggerFactory());

            Assert.True(toTransport.Writer.TryComplete());

            await poll.ProcessRequestAsync(context, context.RequestAborted).OrTimeout();

            Assert.Equal(204, context.Response.StatusCode);
        }

        [Fact]
        public async Task Set200StatusCodeWhenTimeoutTokenFires()
        {
            var toApplication = Channel.CreateUnbounded<byte[]>();
            var toTransport = Channel.CreateUnbounded<byte[]>();
            var context = new DefaultHttpContext();
            var connection = new DefaultConnectionContext("foo", toTransport, toApplication);

            var timeoutToken = new CancellationToken(true);
            var poll = new LongPollingTransport(timeoutToken, toTransport.Reader, connectionId: string.Empty, loggerFactory: new LoggerFactory());

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken, context.RequestAborted))
            {
                await poll.ProcessRequestAsync(context, cts.Token).OrTimeout();

                Assert.Equal(0, context.Response.ContentLength);
                Assert.Equal(200, context.Response.StatusCode);
            }
        }

        [Fact]
        public async Task FrameSentAsSingleResponse()
        {
            var toApplication = Channel.CreateUnbounded<byte[]>();
            var toTransport = Channel.CreateUnbounded<byte[]>();
            var context = new DefaultHttpContext();
            var connection = new DefaultConnectionContext("foo", toTransport, toApplication);

            var poll = new LongPollingTransport(CancellationToken.None, toTransport.Reader, connectionId: string.Empty, loggerFactory: new LoggerFactory());
            var ms = new MemoryStream();
            context.Response.Body = ms;

            await toTransport.Writer.WriteAsync(Encoding.UTF8.GetBytes("Hello World"));

            Assert.True(toTransport.Writer.TryComplete());

            await poll.ProcessRequestAsync(context, context.RequestAborted).OrTimeout();

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal("Hello World", Encoding.UTF8.GetString(ms.ToArray()));
        }

        [Fact]
        public async Task MultipleFramesSentAsSingleResponse()
        {
            var toApplication = Channel.CreateUnbounded<byte[]>();
            var toTransport = Channel.CreateUnbounded<byte[]>();
            var context = new DefaultHttpContext();
            var connection = new DefaultConnectionContext("foo", toTransport, toApplication);

            var poll = new LongPollingTransport(CancellationToken.None, toTransport.Reader, connectionId: string.Empty, loggerFactory: new LoggerFactory());
            var ms = new MemoryStream();
            context.Response.Body = ms;

            await toTransport.Writer.WriteAsync(Encoding.UTF8.GetBytes("Hello"));
            await toTransport.Writer.WriteAsync(Encoding.UTF8.GetBytes(" "));
            await toTransport.Writer.WriteAsync(Encoding.UTF8.GetBytes("World"));

            Assert.True(toTransport.Writer.TryComplete());

            await poll.ProcessRequestAsync(context, context.RequestAborted).OrTimeout();

            Assert.Equal(200, context.Response.StatusCode);

            var payload = ms.ToArray();
            Assert.Equal("Hello World", Encoding.UTF8.GetString(payload));
        }
    }
}
