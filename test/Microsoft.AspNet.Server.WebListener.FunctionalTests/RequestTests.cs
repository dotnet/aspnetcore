// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.PipelineCore;
using Xunit;

namespace Microsoft.AspNet.Server.WebListener
{
    using AppFunc = Func<object, Task>;

    public class RequestTests
    {
        private const string Address = "http://localhost:8080";

        [Fact]
        public async Task Request_SimpleGet_Success()
        {
            using (Utilities.CreateServer("http", "localhost", "8080", "/basepath", env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                try
                {
                    // General keys
                    // TODO: Assert.True(env.Get<CancellationToken>("owin.CallCancelled").CanBeCanceled);

                    var requestInfo = httpContext.GetFeature<IHttpRequestInformation>();

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
                    // TODO: Assert.NotNull(env.Get<IDictionary<string, object>>("server.Capabilities"));

                    var connectionInfo = httpContext.GetFeature<IHttpConnection>();
                    Assert.Equal("::1", connectionInfo.RemoteIpAddress.ToString());
                    Assert.NotEqual(0, connectionInfo.RemotePort);
                    Assert.Equal("::1", connectionInfo.LocalIpAddress.ToString());
                    Assert.NotEqual(0, connectionInfo.LocalPort);
                    Assert.True(connectionInfo.IsLocal);

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
                string response = await SendRequestAsync(Address + "/basepath/SomePath?SomeQuery");
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
            using (Utilities.CreateServer("http", "localhost", "8080", pathBase, env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                try
                {
                    var requestInfo = httpContext.GetFeature<IHttpRequestInformation>();
                    var connectionInfo = httpContext.GetFeature<IHttpConnection>();

                    // Request Keys
                    Assert.Equal("http", requestInfo.Scheme);
                    Assert.Equal(expectedPath, requestInfo.Path);
                    Assert.Equal(expectedPathBase, requestInfo.PathBase);
                    Assert.Equal(string.Empty, requestInfo.QueryString);
                    Assert.Equal(8080, connectionInfo.LocalPort);
                }
                catch (Exception ex)
                {
                    byte[] body = Encoding.ASCII.GetBytes(ex.ToString());
                    httpContext.Response.Body.Write(body, 0, body.Length);
                }
                return Task.FromResult(0);
            }))
            {
                string response = await SendRequestAsync(requestUri);
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
            using (CreateServer(env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                var requestInfo = httpContext.GetFeature<IHttpRequestInformation>();
                try
                {
                    Assert.Equal(expectedPath, requestInfo.Path);
                    Assert.Equal(expectedPathBase, requestInfo.PathBase);
                }
                catch (Exception ex)
                {
                    byte[] body = Encoding.ASCII.GetBytes(ex.ToString());
                    httpContext.Response.Body.Write(body, 0, body.Length);
                }
                return Task.FromResult(0);
            }))
            {
                string response = await SendRequestAsync(Address + requestUri);
                Assert.Equal(string.Empty, response);
            }
        }

        private IDisposable CreateServer(AppFunc app)
        {
            var factory = new ServerFactory(loggerFactory: null);
            var serverInfo = (ServerInformation)factory.Initialize(configuration: null);

            foreach (string path in new[] { "/", "/11", "/2/3", "/2", "/11/2" })
            {
                serverInfo.Listener.UrlPrefixes.Add(UrlPrefix.Create("http", "localhost", "8080", path));
            }

            return factory.Start(serverInfo, app);
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
