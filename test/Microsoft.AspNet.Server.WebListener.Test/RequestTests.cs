// -----------------------------------------------------------------------
// <copyright file="RequestTests.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.Server.WebListener.Tests
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class RequestTests
    {
        private const string Address = "http://localhost:8080";

        [Fact]
        public async Task Request_SimpleGet_Success()
        {
            using (CreateServer(env =>
            {
                try
                {
                    // General keys
                    Assert.Equal("1.0", env.Get<string>("owin.Version"));
                    Assert.True(env.Get<CancellationToken>("owin.CallCancelled").CanBeCanceled);

                    // Request Keys
                    Assert.Equal("GET", env.Get<string>("owin.RequestMethod"));
                    Assert.Equal(Stream.Null, env.Get<Stream>("owin.RequestBody"));
                    Assert.NotNull(env.Get<IDictionary<string, string[]>>("owin.RequestHeaders"));
                    Assert.Equal("http", env.Get<string>("owin.RequestScheme"));
                    Assert.Equal("/basepath", env.Get<string>("owin.RequestPathBase"));
                    Assert.Equal("/SomePath", env.Get<string>("owin.RequestPath"));
                    Assert.Equal("SomeQuery", env.Get<string>("owin.RequestQueryString"));
                    Assert.Equal("HTTP/1.1", env.Get<string>("owin.RequestProtocol"));

                    // Server Keys
                    Assert.NotNull(env.Get<IDictionary<string, object>>("server.Capabilities"));
                    Assert.Equal("::1", env.Get<string>("server.RemoteIpAddress"));
                    Assert.NotNull(env.Get<string>("server.RemotePort"));
                    Assert.Equal("::1", env.Get<string>("server.LocalIpAddress"));
                    Assert.Equal("8080", env.Get<string>("server.LocalPort"));
                    Assert.True(env.Get<bool>("server.IsLocal"));

                    // Note: Response keys are validated in the ResponseTests
                }
                catch (Exception ex)
                {
                    byte[] body = Encoding.ASCII.GetBytes(ex.ToString());
                    env.Get<Stream>("owin.ResponseBody").Write(body, 0, body.Length);
                }
                return Task.FromResult(0);
            }, "http", "localhost", "8080", "/basepath"))
            {
                string response = await SendRequestAsync(Address + "/basepath/SomePath?SomeQuery");
                Assert.Equal(string.Empty, response);
            }
        }

        [Theory]
        [InlineData("http", "localhost", "8080", "/", "http://localhost:8080/", "", "/")]
        [InlineData("http", "localhost", "8080", "/basepath/", "http://localhost:8080/basepath", "/basepath", "")]
        [InlineData("http", "localhost", "8080", "/basepath/", "http://localhost:8080/basepath/", "/basepath", "/")]
        [InlineData("http", "localhost", "8080", "/basepath/", "http://localhost:8080/basepath/subpath", "/basepath", "/subpath")]
        [InlineData("http", "localhost", "8080", "/base path/", "http://localhost:8080/base%20path/sub path", "/base path", "/sub path")]
        [InlineData("http", "localhost", "8080", "/base葉path/", "http://localhost:8080/base%E8%91%89path/sub%E8%91%89path", "/base葉path", "/sub葉path")]
        public async Task Request_PathSplitting(string scheme, string host, string port, string pathBase, string requestUri,
            string expectedPathBase, string expectedPath)
        {
            using (CreateServer(env =>
            {
                try
                {
                    Uri uri = new Uri(requestUri);
                    string expectedQuery = uri.Query.Length > 0 ? uri.Query.Substring(1) : string.Empty;
                    // Request Keys
                    Assert.Equal(scheme, env.Get<string>("owin.RequestScheme"));
                    Assert.Equal(expectedPath, env.Get<string>("owin.RequestPath"));
                    Assert.Equal(expectedPathBase, env.Get<string>("owin.RequestPathBase"));
                    Assert.Equal(expectedQuery, env.Get<string>("owin.RequestQueryString"));
                    Assert.Equal(port, env.Get<string>("server.LocalPort"));
                }
                catch (Exception ex)
                {
                    byte[] body = Encoding.ASCII.GetBytes(ex.ToString());
                    env.Get<Stream>("owin.ResponseBody").Write(body, 0, body.Length);
                }
                return Task.FromResult(0);
            }, scheme, host, port, pathBase))
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
                try
                {
                    Assert.Equal(expectedPath, env.Get<string>("owin.RequestPath"));
                    Assert.Equal(expectedPathBase, env.Get<string>("owin.RequestPathBase"));
                }
                catch (Exception ex)
                {
                    byte[] body = Encoding.ASCII.GetBytes(ex.ToString());
                    env.Get<Stream>("owin.ResponseBody").Write(body, 0, body.Length);
                }
                return Task.FromResult(0);
            }))
            {
                string response = await SendRequestAsync(Address + requestUri);
                Assert.Equal(string.Empty, response);
            }
        }

        private IDisposable CreateServer(AppFunc app, string scheme, string host, string port, string path)
        {
            IDictionary<string, object> properties = new Dictionary<string, object>();
            IList<IDictionary<string, object>> addresses = new List<IDictionary<string, object>>();
            properties["host.Addresses"] = addresses;

            IDictionary<string, object> address = new Dictionary<string, object>();
            addresses.Add(address);

            address["scheme"] = scheme;
            address["host"] = host;
            address["port"] = port;
            address["path"] = path;

            return OwinServerFactory.Create(app, properties);
        }

        private IDisposable CreateServer(AppFunc app)
        {
            IDictionary<string, object> properties = new Dictionary<string, object>();
            IList<IDictionary<string, object>> addresses = new List<IDictionary<string, object>>();
            properties["host.Addresses"] = addresses;

            foreach (string path in new[] { "/", "/11", "/2/3", "/2", "/11/2" })
            {
                IDictionary<string, object> address = new Dictionary<string, object>();
                addresses.Add(address);

                address["scheme"] = "http";
                address["host"] = "localhost";
                address["port"] = "8080";
                address["path"] = path;
            }

            return OwinServerFactory.Create(app, properties);
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
