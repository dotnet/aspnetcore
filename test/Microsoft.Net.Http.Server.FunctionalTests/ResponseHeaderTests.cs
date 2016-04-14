// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Net.Http.Server
{
    public class ResponseHeaderTests
    {
        [Fact]
        public async Task ResponseHeaders_11Request_ServerSendsDefaultHeaders()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                context.Dispose();

                HttpResponseMessage response = await responseTask;
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
        public async Task ResponseHeaders_10Request_ServerSendsDefaultHeaders()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address, usehttp11: false);

                var context = await server.GetContextAsync();
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                response.EnsureSuccessStatusCode();
                Assert.Equal(3, response.Headers.Count());
                Assert.False(response.Headers.TransferEncodingChunked.HasValue);
                Assert.True(response.Headers.ConnectionClose.Value);
                Assert.True(response.Headers.Date.HasValue);
                Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
                Assert.Equal(1, response.Content.Headers.Count());
                Assert.Equal(0, response.Content.Headers.ContentLength);
            }
        }

        [Fact]
        public async Task ResponseHeaders_11HeadRequest_ServerSendsDefaultHeaders()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendHeadRequestAsync(address);

                var context = await server.GetContextAsync();
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                response.EnsureSuccessStatusCode();
                Assert.Equal(3, response.Headers.Count());
                Assert.True(response.Headers.TransferEncodingChunked.Value);
                Assert.True(response.Headers.Date.HasValue);
                Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
                Assert.False(response.Content.Headers.Contains("Content-Length"));
                Assert.Equal(0, response.Content.Headers.Count());
            }
        }

        [Fact]
        public async Task ResponseHeaders_10HeadRequest_ServerSendsDefaultHeaders()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendHeadRequestAsync(address, usehttp11: false);

                var context = await server.GetContextAsync();
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                response.EnsureSuccessStatusCode();
                Assert.Equal(3, response.Headers.Count());
                Assert.False(response.Headers.TransferEncodingChunked.HasValue);
                Assert.True(response.Headers.ConnectionClose.Value);
                Assert.True(response.Headers.Date.HasValue);
                Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
                Assert.False(response.Content.Headers.Contains("Content-Length"));
                Assert.Equal(0, response.Content.Headers.Count());
            }
        }

        [Fact]
        public async Task ResponseHeaders_11HeadRequestWithContentLength_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendHeadRequestAsync(address);

                var context = await server.GetContextAsync();
                context.Response.ContentLength = 20;
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                response.EnsureSuccessStatusCode();
                Assert.Equal(2, response.Headers.Count());
                Assert.False(response.Headers.TransferEncodingChunked.HasValue);
                Assert.True(response.Headers.Date.HasValue);
                Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
                Assert.Equal(1, response.Content.Headers.Count());
                Assert.Equal(20, response.Content.Headers.ContentLength);
            }
        }

        [Fact]
        public async Task ResponseHeaders_11RequestStatusCodeWithoutBody_NoContentLengthOrChunkedOrClose()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                context.Response.StatusCode = 204; // No Content
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                response.EnsureSuccessStatusCode();
                Assert.Equal(2, response.Headers.Count());
                Assert.False(response.Headers.TransferEncodingChunked.HasValue);
                Assert.True(response.Headers.Date.HasValue);
                Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
                Assert.False(response.Content.Headers.Contains("Content-Length"));
                Assert.Equal(0, response.Content.Headers.Count());
            }
        }

        [Fact]
        public async Task ResponseHeaders_11HeadRequestStatusCodeWithoutBody_NoContentLengthOrChunkedOrClose()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendHeadRequestAsync(address);

                var context = await server.GetContextAsync();
                context.Response.StatusCode = 204; // No Content
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                response.EnsureSuccessStatusCode();
                Assert.Equal(2, response.Headers.Count());
                Assert.False(response.Headers.TransferEncodingChunked.HasValue);
                Assert.True(response.Headers.Date.HasValue);
                Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
                Assert.False(response.Content.Headers.Contains("Content-Length"));
                Assert.Equal(0, response.Content.Headers.Count());
            }
        }

        [Fact]
        public async Task ResponseHeaders_ServerSendsSingleValueKnownHeaders_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                WebRequest request = WebRequest.Create(address);
                Task<WebResponse> responseTask = request.GetResponseAsync();

                var context = await server.GetContextAsync();
                var responseHeaders = context.Response.Headers;
                responseHeaders["WWW-Authenticate"] = "custom1";
                context.Dispose();

                // HttpClient would merge the headers no matter what
                HttpWebResponse response = (HttpWebResponse)await responseTask;
                Assert.Equal(4, response.Headers.Count);
                Assert.Null(response.Headers["Transfer-Encoding"]);
                Assert.Equal(0, response.ContentLength);
                Assert.NotNull(response.Headers["Date"]);
                Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers["Server"]);
                Assert.Equal("custom1", response.Headers["WWW-Authenticate"]);
            }
        }

        [Fact]
        public async Task ResponseHeaders_ServerSendsMultiValueKnownHeaders_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                WebRequest request = WebRequest.Create(address);
                Task<WebResponse> responseTask = request.GetResponseAsync();

                var context = await server.GetContextAsync();
                var responseHeaders = context.Response.Headers;
                responseHeaders["WWW-Authenticate"] = new[] { "custom1, and custom2", "custom3" };
                context.Dispose();

                // HttpClient would merge the headers no matter what
                HttpWebResponse response = (HttpWebResponse)await responseTask;
                Assert.Equal(4, response.Headers.Count);
                Assert.Null(response.Headers["Transfer-Encoding"]);
                Assert.Equal(0, response.ContentLength);
                Assert.NotNull(response.Headers["Date"]);
                Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers["Server"]);
#if NETCOREAPP1_0 // WebHeaderCollection.GetValues() not available in CoreCLR.
                Assert.Equal("custom1, and custom2, custom3", response.Headers["WWW-Authenticate"]);
#else
                Assert.Equal(new string[] { "custom1, and custom2", "custom3" }, response.Headers.GetValues("WWW-Authenticate"));
#endif
            }
        }

        [Fact]
        public async Task ResponseHeaders_ServerSendsCustomHeaders_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                WebRequest request = WebRequest.Create(address);
                Task<WebResponse> responseTask = request.GetResponseAsync();

                var context = await server.GetContextAsync();
                var responseHeaders = context.Response.Headers;
                responseHeaders["Custom-Header1"] = new[] { "custom1, and custom2", "custom3" };
                context.Dispose();

                // HttpClient would merge the headers no matter what
                HttpWebResponse response = (HttpWebResponse)await responseTask;
                Assert.Equal(4, response.Headers.Count);
                Assert.Null(response.Headers["Transfer-Encoding"]);
                Assert.Equal(0, response.ContentLength);
                Assert.NotNull(response.Headers["Date"]);
                Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers["Server"]);
#if NETCOREAPP1_0 // WebHeaderCollection.GetValues() not available in CoreCLR.
                Assert.Equal("custom1, and custom2, custom3", response.Headers["Custom-Header1"]);
#else
                Assert.Equal(new string[] { "custom1, and custom2", "custom3" }, response.Headers.GetValues("Custom-Header1"));
#endif
            }
        }

        [Fact]
        public async Task ResponseHeaders_ServerSendsConnectionClose_Closed()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                var responseHeaders = context.Response.Headers;
                responseHeaders["Connection"] = "Close";
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                response.EnsureSuccessStatusCode();
                Assert.True(response.Headers.ConnectionClose.Value);
                Assert.Equal(new string[] { "close" }, response.Headers.GetValues("Connection"));
            }
        }

        [Fact]
        public async Task ResponseHeaders_HTTP10Request_Gets11Close()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address, usehttp11: false);

                var context = await server.GetContextAsync();
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                response.EnsureSuccessStatusCode();
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.True(response.Headers.ConnectionClose.Value);
                Assert.Equal(new string[] { "close" }, response.Headers.GetValues("Connection"));
            }
        }

        [Fact]
        public async Task ResponseHeaders_HTTP10Request_AllowsManualChunking()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, address);
                    request.Version = new Version(1, 0);
                    Task<HttpResponseMessage> responseTask = client.SendAsync(request);

                    var context = await server.GetContextAsync();
                    var responseHeaders = context.Response.Headers;
                    responseHeaders["Transfer-Encoding"] = "chunked";
                    var responseBytes = Encoding.ASCII.GetBytes("10\r\nManually Chunked\r\n0\r\n\r\n");
                    await context.Response.Body.WriteAsync(responseBytes, 0, responseBytes.Length);
                    context.Dispose();

                    HttpResponseMessage response = await responseTask;
                    response.EnsureSuccessStatusCode();
                    Assert.Equal(new Version(1, 1), response.Version);
                    Assert.True(response.Headers.TransferEncodingChunked.Value);
                    Assert.False(response.Content.Headers.Contains("Content-Length"));
                    Assert.True(response.Headers.ConnectionClose.Value);
                    Assert.Equal(new string[] { "close" }, response.Headers.GetValues("Connection"));
                    Assert.Equal("Manually Chunked", await response.Content.ReadAsStringAsync());
                }
            }
        }

        [Fact]
        public async Task ResponseHeaders_HTTP10KeepAliveRequest_Gets11Close()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                // Http.Sys does not support 1.0 keep-alives.
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address, usehttp11: false, sendKeepAlive: true);

                var context = await server.GetContextAsync();
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                response.EnsureSuccessStatusCode();
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.True(response.Headers.ConnectionClose.Value);
            }
        }

        [Fact]
        public async Task Headers_FlushSendsHeaders_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                var responseHeaders = context.Response.Headers;

                responseHeaders["Custom1"] = new[] { "value1a", "value1b" };
                responseHeaders["Custom2"] = "value2a, value2b";
                var body = context.Response.Body;
                Assert.False(context.Response.HasStarted);
                body.Flush();
                Assert.True(context.Response.HasStarted);
                var ex = Assert.Throws<InvalidOperationException>(() => context.Response.StatusCode = 404);
                Assert.Equal("Headers already sent.", ex.Message);
                ex = Assert.Throws<InvalidOperationException>(() => responseHeaders.Add("Custom3", new string[] { "value3a, value3b", "value3c" }));
                Assert.Equal("The response headers cannot be modified because the response has already started.", ex.Message);

                context.Dispose();

                HttpResponseMessage response = await responseTask;
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
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                var responseHeaders = context.Response.Headers;

                responseHeaders["Custom1"] = new[] { "value1a", "value1b" };
                responseHeaders["Custom2"] = "value2a, value2b";
                var body = context.Response.Body;
                Assert.False(context.Response.HasStarted);
                await body.FlushAsync();
                Assert.True(context.Response.HasStarted);
                var ex = Assert.Throws<InvalidOperationException>(() => context.Response.StatusCode = 404);
                Assert.Equal("Headers already sent.", ex.Message);
                ex = Assert.Throws<InvalidOperationException>(() => responseHeaders.Add("Custom3", new string[] { "value3a, value3b", "value3c" }));
                Assert.Equal("The response headers cannot be modified because the response has already started.", ex.Message);

                context.Dispose();

                HttpResponseMessage response = await responseTask;
                response.EnsureSuccessStatusCode();
                Assert.Equal(5, response.Headers.Count()); // Date, Server, Chunked

                Assert.Equal(2, response.Headers.GetValues("Custom1").Count());
                Assert.Equal("value1a", response.Headers.GetValues("Custom1").First());
                Assert.Equal("value1b", response.Headers.GetValues("Custom1").Skip(1).First());
                Assert.Equal(1, response.Headers.GetValues("Custom2").Count());
                Assert.Equal("value2a, value2b", response.Headers.GetValues("Custom2").First());
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string uri, bool usehttp11 = true, bool sendKeepAlive = false)
        {
            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, uri);
                if (!usehttp11)
                {
                    request.Version = new Version(1, 0);
                }
                if (sendKeepAlive)
                {
                    request.Headers.Add("Connection", "Keep-Alive");
                }
                return await client.SendAsync(request);
            }
        }

        private async Task<HttpResponseMessage> SendHeadRequestAsync(string uri, bool usehttp11 = true)
        {
            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Head, uri);
                if (!usehttp11)
                {
                    request.Version = new Version(1, 0);
                }
                return await client.SendAsync(request);
            }
        }
    }
}