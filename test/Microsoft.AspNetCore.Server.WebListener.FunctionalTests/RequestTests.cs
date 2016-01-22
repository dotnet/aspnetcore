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
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.Net.Http.Server;
using Xunit;

namespace Microsoft.AspNet.Server.WebListener
{
    public class RequestTests
    {
        [Fact]
        public async Task Request_SimpleGet_Success()
        {
            string root;
            using (Utilities.CreateHttpServerReturnRoot("/basepath", out root, httpContext =>
            {
                try
                {
                    var requestInfo = httpContext.Features.Get<IHttpRequestFeature>();

                    // Request Keys
                    Assert.Equal("GET", requestInfo.Method);
                    Assert.Equal(Stream.Null, requestInfo.Body);
                    Assert.NotNull(requestInfo.Headers);
                    Assert.Equal("http", requestInfo.Scheme);
                    Assert.Equal("/basepath", requestInfo.PathBase);
                    Assert.Equal("/SomePath", requestInfo.Path);
                    Assert.Equal("?SomeQuery", requestInfo.QueryString);
                    Assert.Equal("HTTP/1.1", requestInfo.Protocol);

                    // Server Keys
                    // TODO: Assert.NotNull(httpContext.Get<IDictionary<string, object>>("server.Capabilities"));

                    var connectionInfo = httpContext.Features.Get<IHttpConnectionFeature>();
                    Assert.Equal("::1", connectionInfo.RemoteIpAddress.ToString());
                    Assert.NotEqual(0, connectionInfo.RemotePort);
                    Assert.Equal("::1", connectionInfo.LocalIpAddress.ToString());
                    Assert.NotEqual(0, connectionInfo.LocalPort);

                    // Trace identifier
                    var requestIdentifierFeature = httpContext.Features.Get<IHttpRequestIdentifierFeature>();
                    Assert.NotNull(requestIdentifierFeature);
                    Assert.NotNull(requestIdentifierFeature.TraceIdentifier);

                    // Note: Response keys are validated in the ResponseTests
                }
                catch (Exception ex)
                {
                    byte[] body = Encoding.ASCII.GetBytes(ex.ToString());
                    httpContext.Response.Body.Write(body, 0, body.Length);
                }
                return Task.FromResult(0);
            }))
            {
                string response = await SendRequestAsync(root + "/basepath/SomePath?SomeQuery");
                Assert.Equal(string.Empty, response);
            }
        }

        [Theory]
        [InlineData("/", "/", "", "/")]
        [InlineData("/basepath/", "/basepath", "/basepath", "")]
        [InlineData("/basepath/", "/basepath/", "/basepath", "/")]
        [InlineData("/basepath/", "/basepath/subpath", "/basepath", "/subpath")]
        [InlineData("/base path/", "/base%20path/sub path", "/base path", "/sub path")]
        [InlineData("/base葉path/", "/base%E8%91%89path/sub%E8%91%89path", "/base葉path", "/sub葉path")]
        [InlineData("/basepath/", "/basepath/sub%2Fpath", "/basepath", "/sub%2Fpath")]
        public async Task Request_PathSplitting(string pathBase, string requestPath, string expectedPathBase, string expectedPath)
        {
            string root;
            using (Utilities.CreateHttpServerReturnRoot(pathBase, out root, httpContext =>
            {
                try
                {
                    var requestInfo = httpContext.Features.Get<IHttpRequestFeature>();
                    var connectionInfo = httpContext.Features.Get<IHttpConnectionFeature>();
                    var requestIdentifierFeature = httpContext.Features.Get<IHttpRequestIdentifierFeature>();

                    // Request Keys
                    Assert.Equal("http", requestInfo.Scheme);
                    Assert.Equal(expectedPath, requestInfo.Path);
                    Assert.Equal(expectedPathBase, requestInfo.PathBase);
                    Assert.Equal(string.Empty, requestInfo.QueryString);

                    // Trace identifier
                    Assert.NotNull(requestIdentifierFeature);
                    Assert.NotNull(requestIdentifierFeature.TraceIdentifier);
                }
                catch (Exception ex)
                {
                    byte[] body = Encoding.ASCII.GetBytes(ex.ToString());
                    httpContext.Response.Body.Write(body, 0, body.Length);
                }
                return Task.FromResult(0);
            }))
            {
                string response = await SendRequestAsync(root + requestPath);
                Assert.Equal(string.Empty, response);
            }
        }

        [Fact]
        public async Task Request_DoubleEscapingAllowed()
        {
            string root;
            using (var server = Utilities.CreateHttpServerReturnRoot("/", out root, httpContext =>
            {
                var requestInfo = httpContext.Features.Get<IHttpRequestFeature>();
                Assert.Equal("/%2F", requestInfo.Path);
                return Task.FromResult(0);
            }))
            {
                var response = await SendSocketRequestAsync(root, "/%252F");
                var responseStatusCode = response.Substring(9); // Skip "HTTP/1.1 "
                Assert.Equal("200", responseStatusCode);
            }
        }

        [Theory]
        // The test server defines these prefixes: "/", "/11", "/2/3", "/2", "/11/2"
        [InlineData("/", "", "/")]
        [InlineData("/random", "", "/random")]
        [InlineData("/11", "/11", "")]
        [InlineData("/11/", "/11", "/")]
        [InlineData("/11/random", "/11", "/random")]
        [InlineData("/2", "/2", "")]
        [InlineData("/2/", "/2", "/")]
        [InlineData("/2/random", "/2", "/random")]
        [InlineData("/2/3", "/2/3", "")]
        [InlineData("/2/3/", "/2/3", "/")]
        [InlineData("/2/3/random", "/2/3", "/random")]
        public async Task Request_MultiplePrefixes(string requestPath, string expectedPathBase, string expectedPath)
        {
            string root;
            using (CreateServer(out root, httpContext =>
            {
                var requestInfo = httpContext.Features.Get<IHttpRequestFeature>();
                var requestIdentifierFeature = httpContext.Features.Get<IHttpRequestIdentifierFeature>();
                try
                {
                    Assert.Equal(expectedPath, requestInfo.Path);
                    Assert.Equal(expectedPathBase, requestInfo.PathBase);

                    // Trace identifier
                    Assert.NotNull(requestIdentifierFeature);
                    Assert.NotNull(requestIdentifierFeature.TraceIdentifier);
                }
                catch (Exception ex)
                {
                    byte[] body = Encoding.ASCII.GetBytes(ex.ToString());
                    httpContext.Response.Body.Write(body, 0, body.Length);
                }
                return Task.FromResult(0);
            }))
            {
                string response = await SendRequestAsync(root + requestPath);
                Assert.Equal(string.Empty, response);
            }
        }

        private IServer CreateServer(out string root, RequestDelegate app)
        {
            // TODO: We're just doing this to get a dynamic port. This can be removed later when we add support for hot-adding prefixes.
            var dynamicServer = Utilities.CreateHttpServerReturnRoot("/", out root, app);
            dynamicServer.Dispose();
            var rootUri = new Uri(root);
            var factory = new ServerFactory(loggerFactory: null);
            var server = factory.CreateServer(configuration: null);
            var listener = server.Features.Get<Microsoft.Net.Http.Server.WebListener>();

            foreach (string path in new[] { "/", "/11", "/2/3", "/2", "/11/2" })
            {
                listener.UrlPrefixes.Add(UrlPrefix.Create(rootUri.Scheme, rootUri.Host, rootUri.Port, path));
            }

            server.Start(new DummyApplication(app));
            return server;
        }

        private async Task<string> SendRequestAsync(string uri)
        {
            using (HttpClient client = new HttpClient())
            {
                return await client.GetStringAsync(uri);
            }
        }

        private async Task<string> SendSocketRequestAsync(string address, string path)
        {
            var uri = new Uri(address);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("GET " + path + " HTTP/1.1");
            builder.AppendLine("Connection: close");
            builder.Append("HOST: ");
            builder.AppendLine(uri.Authority);
            builder.AppendLine();

            byte[] request = Encoding.ASCII.GetBytes(builder.ToString());

            using (var socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect(uri.Host, uri.Port);
                socket.Send(request);
                var response = new byte[12];
                await Task.Run(() => socket.Receive(response));
                return Encoding.ASCII.GetString(response);
            }
        }
    }
}
