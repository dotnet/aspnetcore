// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Net.Server
{
    public class ResponseBodyTests
    {
        private const string Address = "http://localhost:8080/";

        [Fact]
        public async Task ResponseBody_WriteNoHeaders_DefaultsToChunked()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                context.Response.Body.Write(new byte[10], 0, 10);
                await context.Response.Body.WriteAsync(new byte[10], 0, 10);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.Equal(new byte[20], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [Fact]
        public async Task ResponseBody_WriteChunked_Chunked()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                context.Request.Headers["transfeR-Encoding"] = new[] { " CHunked " };
                Stream stream = context.Response.Body;
                stream.EndWrite(stream.BeginWrite(new byte[10], 0, 10, null, null));
                stream.Write(new byte[10], 0, 10);
                await stream.WriteAsync(new byte[10], 0, 10);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.Equal(new byte[30], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [Fact]
        public async Task ResponseBody_WriteContentLength_PassedThrough()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                context.Response.Headers["Content-lenGth"] = new[] { " 30 " };
                Stream stream = context.Response.Body;
                stream.EndWrite(stream.BeginWrite(new byte[10], 0, 10, null, null));
                stream.Write(new byte[10], 0, 10);
                await stream.WriteAsync(new byte[10], 0, 10);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> contentLength;
                Assert.True(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.Equal("30", contentLength.First());
                Assert.Null(response.Headers.TransferEncodingChunked);
                Assert.Equal(new byte[30], await response.Content.ReadAsByteArrayAsync());
            }
        }
        /* TODO: response protocol
        [Fact]
        public async Task ResponseBody_Http10WriteNoHeaders_DefaultsConnectionClose()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                env["owin.ResponseProtocol"] = "HTTP/1.0";
                env.Get<Stream>("owin.ResponseBody").Write(new byte[10], 0, 10);
                return env.Get<Stream>("owin.ResponseBody").WriteAsync(new byte[10], 0, 10);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version); // Http.Sys won't transmit 1.0
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.Null(response.Headers.TransferEncodingChunked);
                Assert.Equal(new byte[20], await response.Content.ReadAsByteArrayAsync());
            }
        }
        */
        /* TODO: Why does this test time out?
        [Fact]
        public async Task ResponseBody_WriteContentLengthNoneWritten_Throws()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                context.Response.Headers["Content-lenGth"] = new[] { " 20 " };
                context.Dispose();

                await Assert.ThrowsAsync<HttpRequestException>(() => responseTask);
            }
        }
        */
        [Fact]
        public async Task ResponseBody_WriteContentLengthNotEnoughWritten_Throws()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                context.Response.Headers["Content-lenGth"] = new[] { " 20 " };
                context.Response.Body.Write(new byte[5], 0, 5);
                context.Dispose();

                await Assert.ThrowsAsync<HttpRequestException>(() => responseTask);
            }
        }

        [Fact]
        public async Task ResponseBody_WriteContentLengthTooMuchWritten_Throws()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                context.Response.Headers["Content-lenGth"] = new[] { " 10 " };
                context.Response.Body.Write(new byte[5], 0, 5);
                Assert.Throws<InvalidOperationException>(() => context.Response.Body.Write(new byte[6], 0, 6));
                context.Dispose();

                await Assert.ThrowsAsync<HttpRequestException>(() => responseTask);
            }
        }

        [Fact]
        public async Task ResponseBody_WriteContentLengthExtraWritten_Throws()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                context.Response.Headers["Content-lenGth"] = new[] { " 10 " };
                context.Response.Body.Write(new byte[10], 0, 10);
                Assert.Throws<ObjectDisposedException>(() => context.Response.Body.Write(new byte[6], 0, 6));
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> contentLength;
                Assert.True(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.Equal("10", contentLength.First());
                Assert.Null(response.Headers.TransferEncodingChunked);
                Assert.Equal(new byte[10], await response.Content.ReadAsByteArrayAsync());
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