// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.PipelineCore;
using Xunit;

namespace Microsoft.AspNet.Server.WebListener
{
    public class RequestHeaderTests
    {
        private const string Address = "http://localhost:8080/";

        [Fact]
        public async Task RequestHeaders_ClientSendsDefaultHeaders_Success()
        {
            using (Utilities.CreateHttpServer(env =>
                {
                    var requestHeaders = new DefaultHttpContext((IFeatureCollection)env).Request.Headers;
                    // NOTE: The System.Net client only sends the Connection: keep-alive header on the first connection per service-point.
                    // Assert.Equal(2, requestHeaders.Count);
                    // Assert.Equal("Keep-Alive", requestHeaders.Get("Connection"));
                    Assert.Equal("localhost:8080", requestHeaders.Get("Host"));
                    Assert.Equal(null, requestHeaders.Get("Accept"));
                    return Task.FromResult(0);
                }))
            {
                string response = await SendRequestAsync(Address);
                Assert.Equal(string.Empty, response);
            }
        }

        [Fact]
        public async Task RequestHeaders_ClientSendsCustomHeaders_Success()
        {
            using (Utilities.CreateHttpServer(env =>
                {
                    var requestHeaders = new DefaultHttpContext((IFeatureCollection)env).Request.Headers;
                    Assert.Equal(4, requestHeaders.Count);
                    Assert.Equal("localhost:8080", requestHeaders.Get("Host"));
                    Assert.Equal("close", requestHeaders.Get("Connection"));
                    Assert.Equal(1, requestHeaders["Custom-Header"].Length);
                    // Apparently Http.Sys squashes request headers together.
                    Assert.Equal("custom1, and custom2, custom3", requestHeaders.Get("Custom-Header"));
                    Assert.Equal(1, requestHeaders["Spacer-Header"].Length);
                    Assert.Equal("spacervalue, spacervalue", requestHeaders.Get("Spacer-Header"));
                    return Task.FromResult(0);
                }))
            {
                string[] customValues = new string[] { "custom1, and custom2", "custom3" };

                await SendRequestAsync("localhost", 8080, "Custom-Header", customValues);
            }
        }
        
        private async Task<string> SendRequestAsync(string uri)
        {
            using (HttpClient client = new HttpClient())
            {
                return await client.GetStringAsync(uri);
            }
        }

        private async Task SendRequestAsync(string host, int port, string customHeader, string[] customValues)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("GET / HTTP/1.1");
            builder.AppendLine("Connection: close");
            builder.Append("HOST: ");
            builder.Append(host);
            builder.Append(':');
            builder.AppendLine(port.ToString());
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
            socket.Connect(host, port);

            socket.Send(request);

            byte[] response = new byte[1024 * 5];
            await Task.Run(() => socket.Receive(response));
            socket.Close();
        }
    }
}
