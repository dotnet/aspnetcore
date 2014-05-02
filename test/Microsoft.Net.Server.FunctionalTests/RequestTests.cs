// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Net.Server
{
    public class RequestTests
    {
        private const string Address = "http://localhost:8080";

        [Fact]
        public async Task Request_SimpleGet_Success()
        {
            using (var server = Utilities.CreateServer("http", "localhost", "8080", "/basepath"))
            {
                Task<string> responseTask = SendRequestAsync(Address + "/basepath/SomePath?SomeQuery");

                var context = await server.GetContextAsync();

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
                Assert.True(request.IsLocal);

                // Note: Response keys are validated in the ResponseTests

                context.Dispose();

                string response = await responseTask;
                Assert.Equal(string.Empty, response);
            }
        }

        [Theory]
        [InlineData("/", "http://localhost:8080/", "", "/")]
        [InlineData("/basepath/", "http://localhost:8080/basepath", "/basepath", "")]
        [InlineData("/basepath/", "http://localhost:8080/basepath/", "/basepath", "/")]
        [InlineData("/basepath/", "http://localhost:8080/basepath/subpath", "/basepath", "/subpath")]
        [InlineData("/base path/", "http://localhost:8080/base%20path/sub path", "/base path", "/sub path")]
        [InlineData("/base葉path/", "http://localhost:8080/base%E8%91%89path/sub%E8%91%89path", "/base葉path", "/sub葉path")]
        public async Task Request_PathSplitting(string pathBase, string requestUri, string expectedPathBase, string expectedPath)
        {
            using (var server = Utilities.CreateServer("http", "localhost", "8080", pathBase))
            {
                Task<string> responseTask = SendRequestAsync(requestUri);

                var context = await server.GetContextAsync();

                // General fields
                var request = context.Request;

                // Request Keys
                Assert.Equal("http", request.Scheme);
                Assert.Equal(expectedPath, request.Path);
                Assert.Equal(expectedPathBase, request.PathBase);
                Assert.Equal(string.Empty, request.QueryString);
                Assert.Equal(8080, request.LocalPort);
                context.Dispose();

                string response = await responseTask;
                Assert.Equal(string.Empty, response);
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
            using (var server = new WebListener())
            {
                foreach (string path in new[] { "/", "/11", "/2/3", "/2", "/11/2" })
                {
                    server.UrlPrefixes.Add(UrlPrefix.Create("http", "localhost", "8080", path));
                }
                server.Start();

                Task<string> responseTask = SendRequestAsync(Address + requestUri);

                var context = await server.GetContextAsync();
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
    }
}
