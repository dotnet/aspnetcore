using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests
{
    public class ServerSentEventsTests
    {
        [Fact]
        public async Task SSESetsContentType()
        {
            using (var factory = new ChannelFactory())
            {
                var connection = new Connection();
                connection.ConnectionId = Guid.NewGuid().ToString();
                var channel = new HttpChannel(factory);
                connection.Channel = channel;
                var sse = new ServerSentEvents(connection);
                var context = new DefaultHttpContext();

                channel.Output.CompleteWriter();

                await sse.ProcessRequest(context);

                Assert.Equal("text/event-stream", context.Response.ContentType);
                Assert.Equal("no-cache", context.Response.Headers["Cache-Control"]);
            }
        }

        [Fact]
        public async Task SSEAddsAppropriateFraming()
        {
            using (var factory = new ChannelFactory())
            {
                var connection = new Connection();
                connection.ConnectionId = Guid.NewGuid().ToString();
                var channel = new HttpChannel(factory);
                connection.Channel = channel;
                var sse = new ServerSentEvents(connection);
                var context = new DefaultHttpContext();
                var ms = new MemoryStream();
                context.Response.Body = ms;

                await channel.Output.WriteAsync(Encoding.UTF8.GetBytes("Hello World"));

                channel.Output.CompleteWriter();

                await sse.ProcessRequest(context);

                var expected = "data: Hello World\n\n";
                Assert.Equal(expected, Encoding.UTF8.GetString(ms.ToArray()));
            }
        }
    }
}
