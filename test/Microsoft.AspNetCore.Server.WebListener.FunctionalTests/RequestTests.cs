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
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Server;
using Xunit;

namespace Microsoft.AspNetCore.Server.WebListener
{
    public class RequestTests
    {
        [Fact]
        public async Task Request_SimpleGet_ExpectedFieldsSet()
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
                    Assert.Equal("/basepath/SomePath?SomeQuery", requestInfo.RawTarget);
                    Assert.Equal("HTTP/1.1", requestInfo.Protocol);

                    var connectionInfo = httpContext.Features.Get<IHttpConnectionFeature>();
                    Assert.Equal("::1", connectionInfo.RemoteIpAddress.ToString());
                    Assert.NotEqual(0, connectionInfo.RemotePort);
                    Assert.Equal("::1", connectionInfo.LocalIpAddress.ToString());
                    Assert.NotEqual(0, connectionInfo.LocalPort);
                    Assert.NotNull(connectionInfo.ConnectionId);

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

        [Fact]
        public async Task Request_FieldsCanBeSet_Set()
        {
            string root;
            using (Utilities.CreateHttpServerReturnRoot("/basepath", out root, httpContext =>
            {
                try
                {
                    var requestInfo = httpContext.Features.Get<IHttpRequestFeature>();

                    // Request Keys
                    requestInfo.Method = "TEST";
                    Assert.Equal("TEST", requestInfo.Method);
                    requestInfo.Body = new MemoryStream();
                    Assert.IsType<MemoryStream>(requestInfo.Body);
                    var customHeaders = new HeaderDictionary(new HeaderCollection());
                    requestInfo.Headers = customHeaders;
                    Assert.Same(customHeaders, requestInfo.Headers);
                    requestInfo.Scheme = "abcd";
                    Assert.Equal("abcd", requestInfo.Scheme);
                    requestInfo.PathBase = "/customized/Base";
                    Assert.Equal("/customized/Base", requestInfo.PathBase);
                    requestInfo.Path = "/customized/Path";
                    Assert.Equal("/customized/Path", requestInfo.Path);
                    requestInfo.QueryString = "?customizedQuery";
                    Assert.Equal("?customizedQuery", requestInfo.QueryString);
                    requestInfo.RawTarget = "/customized/raw?Target";
                    Assert.Equal("/customized/raw?Target", requestInfo.RawTarget);
                    requestInfo.Protocol = "Custom/2.0";
                    Assert.Equal("Custom/2.0", requestInfo.Protocol);

                    var connectionInfo = httpContext.Features.Get<IHttpConnectionFeature>();
                    connectionInfo.RemoteIpAddress = IPAddress.Broadcast;
                    Assert.Equal(IPAddress.Broadcast, connectionInfo.RemoteIpAddress);
                    connectionInfo.RemotePort = 12345;
                    Assert.Equal(12345, connectionInfo.RemotePort);
                    connectionInfo.LocalIpAddress = IPAddress.Any;
                    Assert.Equal(IPAddress.Any, connectionInfo.LocalIpAddress);
                    connectionInfo.LocalPort = 54321;
                    Assert.Equal(54321, connectionInfo.LocalPort);
                    connectionInfo.ConnectionId = "CustomId";
                    Assert.Equal("CustomId", connectionInfo.ConnectionId);

                    // Trace identifier
                    var requestIdentifierFeature = httpContext.Features.Get<IHttpRequestIdentifierFeature>();
                    Assert.NotNull(requestIdentifierFeature);
                    requestIdentifierFeature.TraceIdentifier = "customTrace";
                    Assert.Equal("customTrace", requestIdentifierFeature.TraceIdentifier);

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

        [Fact]
        public async Task Request_FieldsCanBeSetToNull_Set()
        {
            string root;
            using (Utilities.CreateHttpServerReturnRoot("/basepath", out root, httpContext =>
            {
                try
                {
                    var requestInfo = httpContext.Features.Get<IHttpRequestFeature>();

                    // Request Keys
                    requestInfo.Method = null;
                    Assert.Null(requestInfo.Method);
                    requestInfo.Body = null;
                    Assert.Null(requestInfo.Body);
                    requestInfo.Headers = null;
                    Assert.Null(requestInfo.Headers);
                    requestInfo.Scheme = null;
                    Assert.Null(requestInfo.Scheme);
                    requestInfo.PathBase = null;
                    Assert.Null(requestInfo.PathBase);
                    requestInfo.Path = null;
                    Assert.Null(requestInfo.Path);
                    requestInfo.QueryString = null;
                    Assert.Null(requestInfo.QueryString);
                    requestInfo.RawTarget = null;
                    Assert.Null(requestInfo.RawTarget);
                    requestInfo.Protocol = null;
                    Assert.Null(requestInfo.Protocol);

                    var connectionInfo = httpContext.Features.Get<IHttpConnectionFeature>();
                    connectionInfo.RemoteIpAddress = null;
                    Assert.Null(connectionInfo.RemoteIpAddress);
                    connectionInfo.RemotePort = -1;
                    Assert.Equal(-1, connectionInfo.RemotePort);
                    connectionInfo.LocalIpAddress = null;
                    Assert.Null(connectionInfo.LocalIpAddress);
                    connectionInfo.LocalPort = -1;
                    Assert.Equal(-1, connectionInfo.LocalPort);
                    connectionInfo.ConnectionId = null;
                    Assert.Null(connectionInfo.ConnectionId);

                    // Trace identifier
                    var requestIdentifierFeature = httpContext.Features.Get<IHttpRequestIdentifierFeature>();
                    Assert.NotNull(requestIdentifierFeature);
                    requestIdentifierFeature.TraceIdentifier = null;
                    Assert.Null(requestIdentifierFeature.TraceIdentifier);

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
        [InlineData("/base path/", "/base%20path/sub%20path", "/base path", "/sub path")]
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
                    Assert.Equal(requestPath, requestInfo.RawTarget);

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

        [Theory]
        [InlineData("%D0%A4", "Ф")]
        [InlineData("%d0%a4", "Ф")]
        [InlineData("%E0%A4%AD", "भ")]
        [InlineData("%e0%A4%Ad", "भ")]
        [InlineData("%F0%A4%AD%A2", "𤭢")]
        [InlineData("%F0%a4%Ad%a2", "𤭢")]
        [InlineData("%48%65%6C%6C%6F%20%57%6F%72%6C%64", "Hello World")]
        [InlineData("%48%65%6C%6C%6F%2D%C2%B5%40%C3%9F%C3%B6%C3%A4%C3%BC%C3%A0%C3%A1", "Hello-µ@ßöäüàá")]
        // Test the borderline cases of overlong UTF8.
        [InlineData("%C2%80", "\u0080")]
        [InlineData("%E0%A0%80", "\u0800")]
        [InlineData("%F0%90%80%80", "\U00010000")]
        [InlineData("%63", "c")]
        [InlineData("%32", "2")]
        [InlineData("%20", " ")]
        // Mixed
        [InlineData("%%32", "%2")]
        [InlineData("%%20", "% ")]
        public async Task Request_PathDecodingValidUTF8(string requestPath, string expect)
        {
            string root;
            using (var server = Utilities.CreateHttpServerReturnRoot("/", out root, httpContext =>
            {
                Assert.Equal(expect, httpContext.Request.Path.Value.TrimStart('/'));
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(root + "/" + requestPath);
                Assert.Equal(string.Empty, response);
            }
        }

        [Theory]
        [InlineData("%C3%84ra%20Benetton", "Ära Benetton")]
        [InlineData("%E6%88%91%E8%87%AA%E6%A8%AA%E5%88%80%E5%90%91%E5%A4%A9%E7%AC%91%E5%8E%BB%E7%95%99%E8%82%9D%E8%83%86%E4%B8%A4%E6%98%86%E4%BB%91", "我自横刀向天笑去留肝胆两昆仑")]
        public async Task Request_PathDecodingInternationalized(string requestPath, string expect)
        {
            string root;
            using (var server = Utilities.CreateHttpServerReturnRoot("/", out root, httpContext =>
            {
                Assert.Equal(expect, httpContext.Request.Path.Value.TrimStart('/'));
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(root + "/" + requestPath);
                Assert.Equal(string.Empty, response);
            }
        }

        [Theory]
        // Incomplete
        [InlineData("%", "%")]
        [InlineData("%%", "%%")]
        [InlineData("%A", "%A")]
        [InlineData("%Y", "%Y")]
        public async Task Request_PathDecodingInvalidUTF8(string requestPath, string expect)
        {
            string root;
            using (var server = Utilities.CreateHttpServerReturnRoot("/", out root, httpContext =>
            {
                var actualPath = httpContext.Request.Path.Value.TrimStart('/');
                Assert.Equal(expect, actualPath);

                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(root + "/" + requestPath);
                Assert.Equal(string.Empty, response);
            }
        }

        // This test case ensures the consistency of current server behavior through it is not
        // an idea one.
        [Theory]
        // Overlong ASCII
        [InlineData("%C0%A4", true, HttpStatusCode.OK)]
        [InlineData("%C1%BF", true, HttpStatusCode.OK)]
        [InlineData("%E0%80%AF", true, HttpStatusCode.OK)]
        [InlineData("%E0%9F%BF", true, HttpStatusCode.OK)]
        [InlineData("%F0%80%80%AF", true, HttpStatusCode.OK)]
        [InlineData("%F0%8F%8F%BF", false, HttpStatusCode.BadRequest)]
        // Mixed
        [InlineData("%C0%A4%32", true, HttpStatusCode.OK)]
        [InlineData("%32%C0%A4%32", true, HttpStatusCode.OK)]
        [InlineData("%C0%32%A4", true, HttpStatusCode.OK)]
        public async Task Request_ServerErrorFromInvalidUTF8(string requestPath, bool unescaped, HttpStatusCode expectStatus)
        {
            bool pathIsUnescaped = false;
            string root;
            using (var server = Utilities.CreateHttpServerReturnRoot("/", out root, httpContext =>
            {
                var actualPath = httpContext.Request.Path.Value.TrimStart('/');
                pathIsUnescaped = !string.Equals(actualPath, requestPath, StringComparison.Ordinal);
                return Task.FromResult(0);
            }))
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(root + "/" + requestPath);
                    Assert.Equal(expectStatus, response.StatusCode);
                    Assert.Equal(unescaped, pathIsUnescaped);
                }
            }
        }

        [Theory]
        [InlineData("%2F", "%2F")]
        [InlineData("foo%2Fbar", "foo%2Fbar")]
        [InlineData("foo%2F%20bar", "foo%2F bar")]
        public async Task Request_PathDecodingSkipForwardSlash(string requestPath, string expect)
        {
            string root;
            using (var server = Utilities.CreateHttpServerReturnRoot("/", out root, httpContext =>
            {
                Assert.Equal(expect, httpContext.Request.Path.Value.TrimStart('/'));
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(root + "/" + requestPath);
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
                Assert.Equal("/%252F", requestInfo.RawTarget);
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
                    Assert.Equal(requestPath, requestInfo.RawTarget);

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
            var server = new MessagePump(Options.Create(new WebListenerOptions()), new LoggerFactory());

            foreach (string path in new[] { "/", "/11", "/2/3", "/2", "/11/2" })
            {
                server.Listener.Settings.UrlPrefixes.Add(UrlPrefix.Create(rootUri.Scheme, rootUri.Host, rootUri.Port, path));
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
