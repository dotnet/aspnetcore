// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Sockets.Internal.Transports;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests
{
    public class ServerSentEventsTests
    {
        [Fact]
        public async Task SSESetsContentType()
        {
            var toApplication = Channel.CreateUnbounded<byte[]>();
            var toTransport = Channel.CreateUnbounded<byte[]>();
            var context = new DefaultHttpContext();
            var connection = new DefaultConnectionContext("foo", toTransport, toApplication);

            var sse = new ServerSentEventsTransport(toTransport.Reader, connectionId: string.Empty, loggerFactory: new LoggerFactory());

            Assert.True(toTransport.Writer.TryComplete());

            await sse.ProcessRequestAsync(context, context.RequestAborted);

            Assert.Equal("text/event-stream", context.Response.ContentType);
            Assert.Equal("no-cache", context.Response.Headers["Cache-Control"]);
        }

        [Fact]
        public async Task SSETurnsResponseBufferingOff()
        {
            var toApplication = Channel.CreateUnbounded<byte[]>();
            var toTransport = Channel.CreateUnbounded<byte[]>();
            var context = new DefaultHttpContext();
            var connection = new DefaultConnectionContext("foo", toTransport, toApplication);

            var feature = new HttpBufferingFeature();
            context.Features.Set<IHttpBufferingFeature>(feature);
            var sse = new ServerSentEventsTransport(toTransport.Reader, connectionId: string.Empty, loggerFactory: new LoggerFactory());

            Assert.True(toTransport.Writer.TryComplete());

            await sse.ProcessRequestAsync(context, context.RequestAborted);

            Assert.True(feature.ResponseBufferingDisabled);
        }

        [Fact]
        public async Task SSEWritesMessages()
        {
            var toApplication = Channel.CreateUnbounded<byte[]>();
            var toTransport = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
            {
                AllowSynchronousContinuations = true
            });
            var context = new DefaultHttpContext();
            var connection = new DefaultConnectionContext("foo", toTransport, toApplication);

            var ms = new MemoryStream();
            context.Response.Body = ms;
            var sse = new ServerSentEventsTransport(toTransport.Reader, connectionId: string.Empty, loggerFactory: new LoggerFactory());

            var task = sse.ProcessRequestAsync(context, context.RequestAborted);

            await toTransport.Writer.WriteAsync(Encoding.ASCII.GetBytes("Hello"));

            Assert.Equal(":\r\ndata: Hello\r\n\r\n", Encoding.ASCII.GetString(ms.ToArray()));

            toTransport.Writer.TryComplete();

            await task.OrTimeout();
        }

        [Theory]
        [InlineData("Hello World", ":\r\ndata: Hello World\r\n\r\n")]
        [InlineData("Hello\nWorld", ":\r\ndata: Hello\r\ndata: World\r\n\r\n")]
        [InlineData("Hello\r\nWorld", ":\r\ndata: Hello\r\ndata: World\r\n\r\n")]
        public async Task SSEAddsAppropriateFraming(string message, string expected)
        {
            var toApplication = Channel.CreateUnbounded<byte[]>();
            var toTransport = Channel.CreateUnbounded<byte[]>();
            var context = new DefaultHttpContext();
            var connection = new DefaultConnectionContext("foo", toTransport, toApplication);

            var sse = new ServerSentEventsTransport(toTransport.Reader, connectionId: string.Empty, loggerFactory: new LoggerFactory());
            var ms = new MemoryStream();
            context.Response.Body = ms;

            await toTransport.Writer.WriteAsync(Encoding.UTF8.GetBytes(message));

            Assert.True(toTransport.Writer.TryComplete());

            await sse.ProcessRequestAsync(context, context.RequestAborted);

            Assert.Equal(expected, Encoding.UTF8.GetString(ms.ToArray()));
        }

        private class HttpBufferingFeature : IHttpBufferingFeature
        {
            public bool RequestBufferingDisabled { get; set; }

            public bool ResponseBufferingDisabled { get; set; }

            public void DisableRequestBuffering()
            {
                RequestBufferingDisabled = true;
            }

            public void DisableResponseBuffering()
            {
                ResponseBufferingDisabled = true;
            }
        }
    }
}
