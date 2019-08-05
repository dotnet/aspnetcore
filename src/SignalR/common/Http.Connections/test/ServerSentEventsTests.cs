// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Connections.Internal.Transports;
using Xunit;
using Microsoft.AspNetCore.SignalR.Tests;

namespace Microsoft.AspNetCore.Http.Connections.Tests
{
    public class ServerSentEventsTests : VerifiableLoggedTest
    {
        [Fact]
        public async Task SSESetsContentType()
        {
            using (StartVerifiableLog())
            {
                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                var connection = new DefaultConnectionContext("foo", pair.Transport, pair.Application);
                var context = new DefaultHttpContext();

                var sse = new ServerSentEventsServerTransport(connection.Application.Input, connectionId: string.Empty, LoggerFactory);

                connection.Transport.Output.Complete();

                await sse.ProcessRequestAsync(context, context.RequestAborted);

                Assert.Equal("text/event-stream", context.Response.ContentType);
                Assert.Equal("no-cache", context.Response.Headers["Cache-Control"]);
            }
        }

        [Fact]
        public async Task SSETurnsResponseBufferingOff()
        {
            using (StartVerifiableLog())
            {
                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                var connection = new DefaultConnectionContext("foo", pair.Transport, pair.Application);
                var context = new DefaultHttpContext();

                var feature = new HttpBufferingFeature(new MemoryStream());
                context.Features.Set<IHttpResponseBodyFeature>(feature);
                var sse = new ServerSentEventsServerTransport(connection.Application.Input, connectionId: connection.ConnectionId, LoggerFactory);

                connection.Transport.Output.Complete();

                await sse.ProcessRequestAsync(context, context.RequestAborted);

                Assert.True(feature.ResponseBufferingDisabled);
            }
        }

        [Fact]
        public async Task SSEWritesMessages()
        {
            using (StartVerifiableLog())
            {
                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, new PipeOptions(readerScheduler: PipeScheduler.Inline));
                var connection = new DefaultConnectionContext("foo", pair.Transport, pair.Application);
                var context = new DefaultHttpContext();

                var ms = new MemoryStream();
                context.Response.Body = ms;
                var sse = new ServerSentEventsServerTransport(connection.Application.Input, connectionId: string.Empty, LoggerFactory);

                var task = sse.ProcessRequestAsync(context, context.RequestAborted);

                await connection.Transport.Output.WriteAsync(Encoding.ASCII.GetBytes("Hello"));
                connection.Transport.Output.Complete();
                await task.OrTimeout();
                Assert.Equal(":\r\ndata: Hello\r\n\r\n", Encoding.ASCII.GetString(ms.ToArray()));
            }
        }

        [Fact]
        public async Task SSEWritesVeryLargeMessages()
        {
            using (StartVerifiableLog())
            {
                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, new PipeOptions(readerScheduler: PipeScheduler.Inline));
                var connection = new DefaultConnectionContext("foo", pair.Transport, pair.Application);
                var context = new DefaultHttpContext();

                var ms = new MemoryStream();
                context.Response.Body = ms;
                var sse = new ServerSentEventsServerTransport(connection.Application.Input, connectionId: string.Empty, LoggerFactory);

                var task = sse.ProcessRequestAsync(context, context.RequestAborted);

                string hText = new string('H', 60000);
                string wText = new string('W', 60000);

                await connection.Transport.Output.WriteAsync(Encoding.ASCII.GetBytes(hText + wText));
                connection.Transport.Output.Complete();
                await task.OrTimeout();
                Assert.Equal(":\r\ndata: " + hText + wText + "\r\n\r\n", Encoding.ASCII.GetString(ms.ToArray()));
            }
        }

        [Theory]
        [InlineData("Hello World", ":\r\ndata: Hello World\r\n\r\n")]
        [InlineData("Hello\nWorld", ":\r\ndata: Hello\r\ndata: World\r\n\r\n")]
        [InlineData("Hello\r\nWorld", ":\r\ndata: Hello\r\ndata: World\r\n\r\n")]
        public async Task SSEAddsAppropriateFraming(string message, string expected)
        {
            using (StartVerifiableLog())
            {
                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                var connection = new DefaultConnectionContext("foo", pair.Transport, pair.Application);
                var context = new DefaultHttpContext();

                var sse = new ServerSentEventsServerTransport(connection.Application.Input, connectionId: string.Empty, LoggerFactory);
                var ms = new MemoryStream();
                context.Response.Body = ms;

                await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes(message));

                connection.Transport.Output.Complete();

                await sse.ProcessRequestAsync(context, context.RequestAborted);

                Assert.Equal(expected, Encoding.UTF8.GetString(ms.ToArray()));
            }
        }

        private class HttpBufferingFeature : StreamResponseBodyFeature
        {
            public bool ResponseBufferingDisabled { get; set; }

            public HttpBufferingFeature(Stream stream) : base(stream) { }

            public override void DisableBuffering()
            {
                ResponseBufferingDisabled = true;
            }
        }
    }
}
