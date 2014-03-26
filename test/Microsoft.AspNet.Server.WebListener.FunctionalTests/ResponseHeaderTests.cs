// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.PipelineCore;
using Xunit;

namespace Microsoft.AspNet.Server.WebListener
{
    public class ResponseHeaderTests
    {
        private const string Address = "http://localhost:8080/";

        [Fact]
        public async Task ResponseHeaders_ServerSendsDefaultHeaders_Success()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                response.EnsureSuccessStatusCode();
                Assert.Equal(2, response.Headers.Count());
                Assert.False(response.Headers.TransferEncodingChunked.HasValue);
                Assert.True(response.Headers.Date.HasValue);
                Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
                Assert.Equal(1, response.Content.Headers.Count());
                Assert.Equal(0, response.Content.Headers.ContentLength);
            }
        }

        [Fact]
        public async Task ResponseHeaders_ServerSendsSingleValueKnownHeaders_Success()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                var responseInfo = httpContext.GetFeature<IHttpResponseInformation>();
                var responseHeaders = responseInfo.Headers;
                responseHeaders["WWW-Authenticate"] = new string[] { "custom1" };
                return Task.FromResult(0);
            }))
            {
                // HttpClient would merge the headers no matter what
                WebRequest request = WebRequest.Create(Address);
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                Assert.Equal(4, response.Headers.Count);
                Assert.Null(response.Headers["Transfer-Encoding"]);
                Assert.Equal(0, response.ContentLength);
                Assert.NotNull(response.Headers["Date"]);
                Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers["Server"]);
                Assert.Equal(new string[] { "custom1" }, response.Headers.GetValues("WWW-Authenticate"));
            }
        }

        [Fact]
        public async Task ResponseHeaders_ServerSendsMultiValueKnownHeaders_Success()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                var responseInfo = httpContext.GetFeature<IHttpResponseInformation>();
                var responseHeaders = responseInfo.Headers;
                responseHeaders["WWW-Authenticate"] = new string[] { "custom1, and custom2", "custom3" };
                return Task.FromResult(0);
            }))
            {
                // HttpClient would merge the headers no matter what
                WebRequest request = WebRequest.Create(Address);
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                Assert.Equal(4, response.Headers.Count);
                Assert.Null(response.Headers["Transfer-Encoding"]);
                Assert.Equal(0, response.ContentLength);
                Assert.NotNull(response.Headers["Date"]);
                Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers["Server"]);
                Assert.Equal(new string[] { "custom1, and custom2", "custom3" }, response.Headers.GetValues("WWW-Authenticate"));
            }
        }

        [Fact]
        public async Task ResponseHeaders_ServerSendsCustomHeaders_Success()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                var responseInfo = httpContext.GetFeature<IHttpResponseInformation>();
                var responseHeaders = responseInfo.Headers;
                responseHeaders["Custom-Header1"] = new string[] { "custom1, and custom2", "custom3" };
                return Task.FromResult(0);
            }))
            {
                // HttpClient would merge the headers no matter what
                WebRequest request = WebRequest.Create(Address);
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                Assert.Equal(4, response.Headers.Count);
                Assert.Null(response.Headers["Transfer-Encoding"]);
                Assert.Equal(0, response.ContentLength);
                Assert.NotNull(response.Headers["Date"]);
                Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers["Server"]);
                Assert.Equal(new string[] { "custom1, and custom2", "custom3" }, response.Headers.GetValues("Custom-Header1"));
            }
        }

        [Fact]
        public async Task ResponseHeaders_ServerSendsConnectionClose_Closed()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                var responseInfo = httpContext.GetFeature<IHttpResponseInformation>();
                var responseHeaders = responseInfo.Headers;
                responseHeaders["Connection"] = new string[] { "Close" };
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                response.EnsureSuccessStatusCode();
                Assert.True(response.Headers.ConnectionClose.Value);
                Assert.Equal(new string[] { "close" }, response.Headers.GetValues("Connection"));
            }
        }
        /* TODO:
        [Fact]
        public async Task ResponseHeaders_SendsHttp10_Gets11Close()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                env["owin.ResponseProtocol"] = "HTTP/1.0";
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                response.EnsureSuccessStatusCode();
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.True(response.Headers.ConnectionClose.Value);
                Assert.Equal(new string[] { "close" }, response.Headers.GetValues("Connection"));
            }
        }

        [Fact]
        public async Task ResponseHeaders_SendsHttp10WithBody_Gets11Close()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                env["owin.ResponseProtocol"] = "HTTP/1.0";
                return env.Get<Stream>("owin.ResponseBody").WriteAsync(new byte[10], 0, 10);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                response.EnsureSuccessStatusCode();
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.False(response.Headers.TransferEncodingChunked.HasValue);
                Assert.False(response.Content.Headers.Contains("Content-Length"));
                Assert.True(response.Headers.ConnectionClose.Value);
                Assert.Equal(new string[] { "close" }, response.Headers.GetValues("Connection"));
            }
        }
        */

        [Fact]
        public async Task ResponseHeaders_HTTP10Request_Gets11Close()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                return Task.FromResult(0);
            }))
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, Address);
                    request.Version = new Version(1, 0);
                    HttpResponseMessage response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    Assert.Equal(new Version(1, 1), response.Version);
                    Assert.True(response.Headers.ConnectionClose.Value);
                    Assert.Equal(new string[] { "close" }, response.Headers.GetValues("Connection"));
                }
            }
        }

        [Fact]
        public async Task ResponseHeaders_HTTP10Request_RemovesChunkedHeader()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                var responseInfo = httpContext.GetFeature<IHttpResponseInformation>();
                var responseHeaders = responseInfo.Headers;
                responseHeaders["Transfer-Encoding"] = new string[] { "chunked" };
                return responseInfo.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, Address);
                    request.Version = new Version(1, 0);
                    HttpResponseMessage response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    Assert.Equal(new Version(1, 1), response.Version);
                    Assert.False(response.Headers.TransferEncodingChunked.HasValue);
                    Assert.False(response.Content.Headers.Contains("Content-Length"));
                    Assert.True(response.Headers.ConnectionClose.Value);
                    Assert.Equal(new string[] { "close" }, response.Headers.GetValues("Connection"));
                }
            }
        }

        [Fact]
        public async Task Headers_FlushSendsHeaders_Success()
        {
            using (Utilities.CreateHttpServer(
                env =>
                {
                    var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                    var responseInfo = httpContext.GetFeature<IHttpResponseInformation>();
                    var responseHeaders = responseInfo.Headers;
                    responseHeaders.Add("Custom1", new string[] { "value1a", "value1b" });
                    responseHeaders.Add("Custom2", new string[] { "value2a, value2b" });
                    var body = responseInfo.Body;
                    body.Flush();
                    Assert.Throws<InvalidOperationException>(() => responseInfo.StatusCode = 404);
                    responseHeaders.Add("Custom3", new string[] { "value3a, value3b", "value3c" }); // Ignored
                    return Task.FromResult(0);
                }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                response.EnsureSuccessStatusCode();
                Assert.Equal(5, response.Headers.Count()); // Date, Server, Chunked

                Assert.Equal(2, response.Headers.GetValues("Custom1").Count());
                Assert.Equal("value1a", response.Headers.GetValues("Custom1").First());
                Assert.Equal("value1b", response.Headers.GetValues("Custom1").Skip(1).First());
                Assert.Equal(1, response.Headers.GetValues("Custom2").Count());
                Assert.Equal("value2a, value2b", response.Headers.GetValues("Custom2").First());
            }
        }

        [Fact]
        public async Task Headers_FlushAsyncSendsHeaders_Success()
        {
            using (Utilities.CreateHttpServer(
                async env =>
                {
                    var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                    var responseInfo = httpContext.GetFeature<IHttpResponseInformation>();
                    var responseHeaders = responseInfo.Headers;
                    responseHeaders.Add("Custom1", new string[] { "value1a", "value1b" });
                    responseHeaders.Add("Custom2", new string[] { "value2a, value2b" });
                    var body = responseInfo.Body;
                    await body.FlushAsync();
                    Assert.Throws<InvalidOperationException>(() => responseInfo.StatusCode = 404);
                    responseHeaders.Add("Custom3", new string[] { "value3a, value3b", "value3c" }); // Ignored
                }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                response.EnsureSuccessStatusCode();
                Assert.Equal(5, response.Headers.Count()); // Date, Server, Chunked

                Assert.Equal(2, response.Headers.GetValues("Custom1").Count());
                Assert.Equal("value1a", response.Headers.GetValues("Custom1").First());
                Assert.Equal("value1b", response.Headers.GetValues("Custom1").Skip(1).First());
                Assert.Equal(1, response.Headers.GetValues("Custom2").Count());
                Assert.Equal("value2a, value2b", response.Headers.GetValues("Custom2").First());
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string uri)
        {
            using (HttpClient client = new HttpClient())
            {
                return await client.GetAsync(uri);
            }
        }
    }
}
