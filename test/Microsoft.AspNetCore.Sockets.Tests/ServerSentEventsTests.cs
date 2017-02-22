// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Sockets.Transports;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests
{
    public class ServerSentEventsTests
    {
        [Fact]
        public async Task SSESetsContentType()
        {
            var channel = Channel.CreateUnbounded<Message>();
            var context = new DefaultHttpContext();
            var sse = new ServerSentEventsTransport(channel, new LoggerFactory());

            Assert.True(channel.Out.TryComplete());

            await sse.ProcessRequestAsync(context, context.RequestAborted);

            Assert.Equal("text/event-stream", context.Response.ContentType);
            Assert.Equal("no-cache", context.Response.Headers["Cache-Control"]);
        }

        [Fact]
        public async Task SSEAddsAppropriateFraming()
        {
            var channel = Channel.CreateUnbounded<Message>();
            var context = new DefaultHttpContext();
            var sse = new ServerSentEventsTransport(channel, new LoggerFactory());
            var ms = new MemoryStream();
            context.Response.Body = ms;

            await channel.Out.WriteAsync(new Message(
                Encoding.UTF8.GetBytes("Hello World"),
                MessageType.Text,
                endOfMessage: true));

            Assert.True(channel.Out.TryComplete());

            await sse.ProcessRequestAsync(context, context.RequestAborted);

            var expected = "data: Hello World\n\n";
            Assert.Equal(expected, Encoding.UTF8.GetString(ms.ToArray()));
        }
    }
}
