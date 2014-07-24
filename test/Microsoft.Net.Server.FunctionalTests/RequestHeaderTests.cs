// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Net.Server
{
    public class RequestHeaderTests
    {
        [Fact]
        public async Task RequestHeaders_ClientSendsDefaultHeaders_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<string> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                var requestHeaders = context.Request.Headers;
                // NOTE: The System.Net client only sends the Connection: keep-alive header on the first connection per service-point.
                // Assert.Equal(2, requestHeaders.Count);
                // Assert.Equal("Keep-Alive", requestHeaders.Get("Connection"));
                Assert.Equal(new Uri(address).Authority, requestHeaders["Host"]);
                string[] values;
                Assert.False(requestHeaders.TryGetValue("Accept", out values));
                Assert.False(requestHeaders.ContainsKey("Accept"));
                Assert.Null(requestHeaders["Accept"]);
                context.Dispose();

                string response = await responseTask;
                Assert.Equal(string.Empty, response);
            }
        }

        [Fact]
        public async Task RequestHeaders_ClientSendsCustomHeaders_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                string[] customValues = new string[] { "custom1, and custom2", "custom3" };
                Task responseTask = SendRequestAsync(address, "Custom-Header", customValues);

                var context = await server.GetContextAsync();
                var requestHeaders = context.Request.Headers;
                Assert.Equal(4, requestHeaders.Count);
                Assert.Equal(new Uri(address).Authority, requestHeaders["Host"]);
                Assert.Equal(new[] { new Uri(address).Authority }, requestHeaders.GetValues("Host"));
                Assert.Equal("close", requestHeaders["Connection"]);
                Assert.Equal(new[] { "close" }, requestHeaders.GetValues("Connection"));
                // Apparently Http.Sys squashes request headers together.
                Assert.Equal("custom1, and custom2, custom3", requestHeaders["Custom-Header"]);
                Assert.Equal(new[] { "custom1", "and custom2", "custom3" }, requestHeaders.GetValues("Custom-Header"));
                Assert.Equal("spacervalue, spacervalue", requestHeaders["Spacer-Header"]);
                Assert.Equal(new[] { "spacervalue", "spacervalue" }, requestHeaders.GetValues("Spacer-Header"));
                context.Dispose();

                await responseTask;
            }
        }
        
        private async Task<string> SendRequestAsync(string uri)
        {
            using (HttpClient client = new HttpClient())
            {
                return await client.GetStringAsync(uri);
            }
        }

        private async Task SendRequestAsync(string address, string customHeader, string[] customValues)
        {
            var uri = new Uri(address);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("GET / HTTP/1.1");
            builder.AppendLine("Connection: close");
            builder.Append("HOST: ");
            builder.AppendLine(uri.Authority);
            foreach (string value in customValues)
            {
                builder.Append(customHeader);
                builder.Append(": ");
                builder.AppendLine(value);
                builder.AppendLine("Spacer-Header: spacervalue");
            }
            builder.AppendLine();

            byte[] request = Encoding.ASCII.GetBytes(builder.ToString());

            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(uri.Host, uri.Port);

            socket.Send(request);

            byte[] response = new byte[1024 * 5];
            await Task.Run(() => socket.Receive(response));
            socket.Close();
        }
    }
}