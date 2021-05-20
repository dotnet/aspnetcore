// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.Listener
{
    public class RequestHeaderTests
    {

        [ConditionalFact]
        public async Task RequestHeaders_ClientSendsUtf8Headers_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                string[] customValues = new string[] { "custom1, and custom测试2", "custom3" };
                Task responseTask = SendRequestAsync(address, "Custom-Header", customValues);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                var requestHeaders = context.Request.Headers;
                Assert.Equal(4, requestHeaders.Count);
                Assert.Equal(new Uri(address).Authority, requestHeaders["Host"]);
                Assert.Equal(new[] { new Uri(address).Authority }, requestHeaders.GetValues("Host"));
                Assert.Equal("close", requestHeaders["Connection"]);
                Assert.Equal(new[] { "close" }, requestHeaders.GetValues("Connection"));
                // Apparently Http.Sys squashes request headers together.
                Assert.Equal("custom1, and custom测试2, custom3", requestHeaders["Custom-Header"]);
                Assert.Equal(new[] { "custom1", "and custom测试2", "custom3" }, requestHeaders.GetValues("Custom-Header"));
                Assert.Equal("spacervalue, spacervalue", requestHeaders["Spacer-Header"]);
                Assert.Equal(new[] { "spacervalue", "spacervalue" }, requestHeaders.GetValues("Spacer-Header"));
                context.Dispose();

                await responseTask;
            }
        }

        [ConditionalFact]
        public async Task RequestHeaders_ClientSendsKnownHeaderWithNoValue_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                string[] customValues = new string[] { "" };
                Task responseTask = SendRequestAsync(address, "If-None-Match", customValues);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                var requestHeaders = context.Request.Headers;
                Assert.Equal(3, requestHeaders.Count);
                Assert.Equal(new Uri(address).Authority, requestHeaders["Host"]);
                Assert.Equal(new[] { new Uri(address).Authority }, requestHeaders.GetValues("Host"));
                Assert.Equal("close", requestHeaders["Connection"]);
                Assert.Equal(new[] { "close" }, requestHeaders.GetValues("Connection"));
                Assert.Equal(StringValues.Empty, requestHeaders["If-None-Match"]);
                Assert.Empty(requestHeaders.GetValues("If-None-Match"));
                Assert.Equal("spacervalue", requestHeaders["Spacer-Header"]);
                context.Dispose();

                await responseTask;
            }
        }

        [ConditionalFact]
        public async Task RequestHeaders_ClientSendsUnknownHeaderWithNoValue_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                string[] customValues = new string[] { "" };
                Task responseTask = SendRequestAsync(address, "Custom-Header", customValues);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                var requestHeaders = context.Request.Headers;
                Assert.Equal(4, requestHeaders.Count);
                Assert.Equal(new Uri(address).Authority, requestHeaders["Host"]);
                Assert.Equal(new[] { new Uri(address).Authority }, requestHeaders.GetValues("Host"));
                Assert.Equal("close", requestHeaders["Connection"]);
                Assert.Equal(new[] { "close" }, requestHeaders.GetValues("Connection"));
                Assert.Equal("", requestHeaders["Custom-Header"]);
                Assert.Empty(requestHeaders.GetValues("Custom-Header"));
                Assert.Equal("spacervalue", requestHeaders["Spacer-Header"]);
                context.Dispose();

                await responseTask;
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

            byte[] request = Encoding.UTF8.GetBytes(builder.ToString());

            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(uri.Host, uri.Port);

            socket.Send(request);

            byte[] response = new byte[1024 * 5];
            await Task.Run(() => socket.Receive(response));
            socket.Dispose();
        }
    }
}