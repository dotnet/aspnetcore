// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR.Tests.Common;
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
            var channel = Channel.CreateUnbounded<byte[]>();
            var context = new DefaultHttpContext();
            var poll = new LongPollingTransport(CancellationToken.None, channel, connectionId: string.Empty, loggerFactory: new LoggerFactory());

            Assert.True(channel.Out.TryComplete());

            await poll.ProcessRequestAsync(context, context.RequestAborted);

            Assert.Equal(204, context.Response.StatusCode);
        }

        [Fact]
        public async Task Set200StatusCodeWhenTimeoutTokenFires()
        {
            var channel = Channel.CreateUnbounded<byte[]>();
            var context = new DefaultHttpContext();
            var timeoutToken = new CancellationToken(true);
            var poll = new LongPollingTransport(timeoutToken, channel, connectionId: string.Empty, loggerFactory: new LoggerFactory());

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
            var channel = Channel.CreateUnbounded<byte[]>();
            var context = new DefaultHttpContext();
            var poll = new LongPollingTransport(CancellationToken.None, channel, connectionId: string.Empty, loggerFactory: new LoggerFactory());
            var ms = new MemoryStream();
            context.Response.Body = ms;

            await channel.Out.WriteAsync(Encoding.UTF8.GetBytes("Hello World"));

            Assert.True(channel.Out.TryComplete());

            await poll.ProcessRequestAsync(context, context.RequestAborted);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal("Hello World", Encoding.UTF8.GetString(ms.ToArray()));
        }

        [Fact]
        public async Task MultipleFramesSentAsSingleResponse()
        {
            var channel = Channel.CreateUnbounded<byte[]>();
            var context = new DefaultHttpContext();

            var poll = new LongPollingTransport(CancellationToken.None, channel, connectionId: string.Empty, loggerFactory: new LoggerFactory());
            var ms = new MemoryStream();
            context.Response.Body = ms;

            await channel.Out.WriteAsync(Encoding.UTF8.GetBytes("Hello"));
            await channel.Out.WriteAsync(Encoding.UTF8.GetBytes(" "));
            await channel.Out.WriteAsync(Encoding.UTF8.GetBytes("World"));

            Assert.True(channel.Out.TryComplete());

            await poll.ProcessRequestAsync(context, context.RequestAborted);

            Assert.Equal(200, context.Response.StatusCode);

            var payload = ms.ToArray();
            Assert.Equal("Hello World", Encoding.UTF8.GetString(payload));
        }
    }
}
