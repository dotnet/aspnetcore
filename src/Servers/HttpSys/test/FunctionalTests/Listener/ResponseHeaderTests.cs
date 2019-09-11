// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.Listener
{
    public class ResponseHeaderTests : IDisposable
    {
        private HttpClient _client = new HttpClient();

        void IDisposable.Dispose()
        {
            _client.Dispose();
        }

        [ConditionalFact]
        public async Task ResponseHeaders_11Request_ServerSendsDefaultHeaders()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                response.EnsureSuccessStatusCode();
                Assert.Equal(2, response.Headers.Count());
                Assert.False(response.Headers.TransferEncodingChunked.HasValue);
                Assert.True(response.Headers.Date.HasValue);
                Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
                Assert.Single(response.Content.Headers);
                Assert.Equal(0, response.Content.Headers.ContentLength);
            }
        }

        [ConditionalFact]
        public async Task ResponseHeaders_10Request_ServerSendsDefaultHeaders()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address, usehttp11: false);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                response.EnsureSuccessStatusCode();
                Assert.Equal(3, response.Headers.Count());
                Assert.False(response.Headers.TransferEncodingChunked.HasValue);
                Assert.True(response.Headers.ConnectionClose.Value);
                Assert.True(response.Headers.Date.HasValue);
                Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
                Assert.Single(response.Content.Headers);
                Assert.Equal(0, response.Content.Headers.ContentLength);
            }
        }

        [ConditionalFact]
        public async Task ResponseHeaders_11HeadRequest_ServerSendsDefaultHeaders()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendHeadRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                response.EnsureSuccessStatusCode();
                Assert.Equal(2, response.Headers.Count());
                Assert.False(response.Headers.TransferEncodingChunked.HasValue);
                Assert.True(response.Headers.Date.HasValue);
                Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
                Assert.False(response.Content.Headers.Contains("Content-Length"));
                Assert.Empty(response.Content.Headers);

                // Send a second request to check that the connection wasn't corrupted.
                responseTask = SendHeadRequestAsync(address);
                context = await server.AcceptAsync(Utilities.DefaultTimeout);
                context.Dispose();
                response = await responseTask;
            }
        }

        [ConditionalFact]
        public async Task ResponseHeaders_10HeadRequest_ServerSendsDefaultHeaders()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendHeadRequestAsync(address, usehttp11: false);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                response.EnsureSuccessStatusCode();
                Assert.Equal(3, response.Headers.Count());
                Assert.False(response.Headers.TransferEncodingChunked.HasValue);
                Assert.True(response.Headers.ConnectionClose.Value);
                Assert.True(response.Headers.Date.HasValue);
                Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
                Assert.False(response.Content.Headers.Contains("Content-Length"));
                Assert.Empty(response.Content.Headers);

                // Send a second request to check that the connection wasn't corrupted.
                responseTask = SendHeadRequestAsync(address);
                context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Dispose();
                response = await responseTask;
            }
        }

        [ConditionalFact]
        public async Task ResponseHeaders_11HeadRequestWithContentLength_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendHeadRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.ContentLength = 20;
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                response.EnsureSuccessStatusCode();
                Assert.Equal(2, response.Headers.Count());
                Assert.False(response.Headers.TransferEncodingChunked.HasValue);
                Assert.True(response.Headers.Date.HasValue);
                Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
                Assert.Single(response.Content.Headers);
                Assert.Equal(20, response.Content.Headers.ContentLength);

                // Send a second request to check that the connection wasn't corrupted.
                responseTask = SendHeadRequestAsync(address);
                context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Dispose();
                response = await responseTask;
            }
        }

        [ConditionalFact]
        public async Task ResponseHeaders_11RequestStatusCodeWithoutBody_NoContentLengthOrChunkedOrClose()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.StatusCode = 204; // No Content
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                response.EnsureSuccessStatusCode();
                Assert.Equal(2, response.Headers.Count());
                Assert.False(response.Headers.TransferEncodingChunked.HasValue);
                Assert.True(response.Headers.Date.HasValue);
                Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
                Assert.False(response.Content.Headers.Contains("Content-Length"));
                Assert.Empty(response.Content.Headers);
            }
        }

        [ConditionalFact]
        public async Task ResponseHeaders_11HeadRequestStatusCodeWithoutBody_NoContentLengthOrChunkedOrClose()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendHeadRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.StatusCode = 204; // No Content
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                response.EnsureSuccessStatusCode();
                Assert.Equal(2, response.Headers.Count());
                Assert.False(response.Headers.TransferEncodingChunked.HasValue);
                Assert.True(response.Headers.Date.HasValue);
                Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
                Assert.False(response.Content.Headers.Contains("Content-Length"));
                Assert.Empty(response.Content.Headers);

                // Send a second request to check that the connection wasn't corrupted.
                responseTask = SendHeadRequestAsync(address);
                context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Dispose();
                response = await responseTask;
            }
        }

        [ConditionalFact]
        public async Task ResponseHeaders_HTTP10KeepAliveRequest_Gets11Close()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                // Http.Sys does not support 1.0 keep-alives.
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address, usehttp11: false, sendKeepAlive: true);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                response.EnsureSuccessStatusCode();
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.True(response.Headers.ConnectionClose.Value);
            }
        }

        [ConditionalTheory]
        [InlineData("Server", "\r\nData")]
        [InlineData("Server", "\0Data")]
        [InlineData("Server", "Data\r")]
        [InlineData("Server", "Da\0ta")]
        [InlineData("Server", "Da\u001Fta")]
        [InlineData("Unknown-Header", "\r\nData")]
        [InlineData("Unknown-Header", "\0Data")]
        [InlineData("Unknown-Header", "Data\0")]
        [InlineData("Unknown-Header", "Da\nta")]
        [InlineData("\r\nServer", "Data")]
        [InlineData("Server\r", "Data")]
        [InlineData("Ser\0ver", "Data")]
        [InlineData("Server\r\n", "Data")]
        [InlineData("\u001FServer", "Data")]
        [InlineData("Unknown-Header\r\n", "Data")]
        [InlineData("\0Unknown-Header", "Data")]
        [InlineData("Unknown\r-Header", "Data")]
        [InlineData("Unk\nown-Header", "Data")]
        public async Task AddingControlCharactersToHeadersThrows(string key, string value)
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);

                var responseHeaders = context.Response.Headers;

                Assert.Throws<InvalidOperationException>(() => {
                    responseHeaders[key] = value;
                });

                Assert.Throws<InvalidOperationException>(() => {
                    responseHeaders[key] = new StringValues(new[] { "valid", value });
                });

                Assert.Throws<InvalidOperationException>(() => {
                    ((IDictionary<string, StringValues>)responseHeaders)[key] = value;
                });

                Assert.Throws<InvalidOperationException>(() => {
                    var kvp = new KeyValuePair<string, StringValues>(key, value);
                    ((ICollection<KeyValuePair<string, StringValues>>)responseHeaders).Add(kvp);
                });

                Assert.Throws<InvalidOperationException>(() => {
                    var kvp = new KeyValuePair<string, StringValues>(key, value);
                    ((IDictionary<string, StringValues>)responseHeaders).Add(key, value);
                });

                context.Dispose();

                HttpResponseMessage response = await responseTask;
                response.EnsureSuccessStatusCode();
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string uri, bool usehttp11 = true, bool sendKeepAlive = false)
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
            return await _client.SendAsync(request);
        }

        private async Task<HttpResponseMessage> SendHeadRequestAsync(string uri, bool usehttp11 = true)
        {
            var request = new HttpRequestMessage(HttpMethod.Head, uri);
            if (!usehttp11)
            {
                request.Version = new Version(1, 0);
            }
            return await _client.SendAsync(request);
        }
    }
}