// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Net.Http.Server
{
    public class RequestTests
    {
        [Fact]
        public async Task Request_SimpleGet_Success()
        {
            string root;
            using (var server = Utilities.CreateHttpServerReturnRoot("/basepath", out root))
            {
                Task<string> responseTask = SendRequestAsync(root + "/basepath/SomePath?SomeQuery");

                var context = await server.AcceptAsync();

                // General fields
                var request = context.Request;

                // Request Keys
                Assert.Equal("GET", request.Method);
                Assert.Equal(Stream.Null, request.Body);
                Assert.NotNull(request.Headers);
                Assert.Equal("http", request.Scheme);
                Assert.Equal("/basepath", request.PathBase);
                Assert.Equal("/SomePath", request.Path);
                Assert.Equal("?SomeQuery", request.QueryString);
                Assert.Equal(new Version(1, 1), request.ProtocolVersion);

                Assert.Equal("::1", request.RemoteIpAddress.ToString());
                Assert.NotEqual(0, request.RemotePort);
                Assert.Equal("::1", request.LocalIpAddress.ToString());
                Assert.NotEqual(0, request.LocalPort);

                // Note: Response keys are validated in the ResponseTests

                context.Dispose();

                string response = await responseTask;
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
            using (var server = Utilities.CreateHttpServerReturnRoot(pathBase, out root))
            {
                Task<string> responseTask = SendRequestAsync(root + requestPath);

                var context = await server.AcceptAsync();

                // General fields
                var request = context.Request;

                // Request Keys
                Assert.Equal("http", request.Scheme);
                Assert.Equal(expectedPath, request.Path);
                Assert.Equal(expectedPathBase, request.PathBase);
                Assert.Equal(string.Empty, request.QueryString);
                context.Dispose();

                string response = await responseTask;
                Assert.Equal(string.Empty, response);
            }
        }

        [Theory]
        [InlineData("/path%")]
        [InlineData("/path%XY")]
        [InlineData("/path%F")]
        [InlineData("/path with spaces")]
        public async Task Request_MalformedPathReturns400StatusCode(string requestPath)
        {
            string root;
            using (var server = Utilities.CreateHttpServerReturnRoot("/", out root))
            {
                var responseTask = SendSocketRequestAsync(root, requestPath);
                var contextTask = server.AcceptAsync();
                var response = await responseTask;
                var responseStatusCode = response.Substring(9); // Skip "HTTP/1.1 "
                Assert.Equal("400", responseStatusCode);
            }
        }

        [Fact]
        public async Task Request_DoubleEscapingAllowed()
        {
            string root;
            using (var server = Utilities.CreateHttpServerReturnRoot("/", out root))
            {
                var responseTask = SendSocketRequestAsync(root, "/%252F");
                var context = await server.AcceptAsync();
                Assert.Equal("/%2F", context.Request.Path);
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
        public async Task Request_MultiplePrefixes(string requestUri, string expectedPathBase, string expectedPath)
        {
            // TODO: We're just doing this to get a dynamic port. This can be removed later when we add support for hot-adding prefixes.
            string root;
            var server = Utilities.CreateHttpServerReturnRoot("/", out root);
            server.Dispose();
            server = new WebListener();
            using (server)
            {
                var uriBuilder = new UriBuilder(root);
                foreach (string path in new[] { "/", "/11", "/2/3", "/2", "/11/2" })
                {
                    server.Settings.UrlPrefixes.Add(UrlPrefix.Create(uriBuilder.Scheme, uriBuilder.Host, uriBuilder.Port, path));
                }
                server.Start();

                Task<string> responseTask = SendRequestAsync(root + requestUri);

                var context = await server.AcceptAsync();
                var request = context.Request;

                Assert.Equal(expectedPath, request.Path);
                Assert.Equal(expectedPathBase, request.PathBase);

                context.Dispose();

                string response = await responseTask;
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
