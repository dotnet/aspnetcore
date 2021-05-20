// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    public class RequestHeaderTests
    {
        [ConditionalFact]
        public async Task RequestHeaders_ClientSendsDefaultHeaders_Success()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
                {
                    var requestHeaders = httpContext.Request.Headers;
                    // NOTE: The System.Net client only sends the Connection: keep-alive header on the first connection per service-point.
                    // Assert.Equal(2, requestHeaders.Count);
                    // Assert.Equal("Keep-Alive", requestHeaders.Get("Connection"));
                    Assert.False(StringValues.IsNullOrEmpty(requestHeaders["Host"]));
                    Assert.True(StringValues.IsNullOrEmpty(requestHeaders["Accept"]));
                    return Task.FromResult(0);
                }))
            {
                string response = await SendRequestAsync(address);
                Assert.Equal(string.Empty, response);
            }
        }

        [ConditionalFact]
        public async Task RequestHeaders_ClientSendsCustomHeaders_Success()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
                {
                    var requestHeaders = httpContext.Request.Headers;
                    Assert.Equal(4, requestHeaders.Count);
                    Assert.False(StringValues.IsNullOrEmpty(requestHeaders["Host"]));
                    Assert.Equal("close", requestHeaders["Connection"]);
                    // Apparently Http.Sys squashes request headers together.
                    Assert.Single(requestHeaders["Custom-Header"]);
                    Assert.Equal("custom1, and custom2, custom3", requestHeaders["Custom-Header"]);
                    Assert.Single(requestHeaders["Spacer-Header"]);
                    Assert.Equal("spacervalue, spacervalue", requestHeaders["Spacer-Header"]);
                    return Task.FromResult(0);
                }))
            {
                string[] customValues = new string[] { "custom1, and custom2", "custom3" };

                await SendRequestAsync(address, "Custom-Header", customValues);
            }
        }

        [ConditionalFact]
        public async Task RequestHeaders_ServerAddsCustomHeaders_Success()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                var requestHeaders = httpContext.Request.Headers;
                var header = KeyValuePair.Create("Custom-Header", new StringValues("custom"));
                requestHeaders.Add(header);

                Assert.True(requestHeaders.Contains(header));
                return Task.FromResult(0);
            }))
            {
                string response = await SendRequestAsync(address);
                Assert.Equal(string.Empty, response);
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
            socket.Dispose();
        }
    }
}
