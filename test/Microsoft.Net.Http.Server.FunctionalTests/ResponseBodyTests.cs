// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.Net.Http.Server
{
    public class ResponseBodyTests
    {
        [Fact]
        public async Task ResponseBody_BufferWriteNoHeaders_DefaultsToContentLength()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                context.Response.ShouldBuffer = true;
                context.Response.Body.Write(new byte[10], 0, 10);
                await context.Response.Body.WriteAsync(new byte[10], 0, 10);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> ignored;
                Assert.True(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.False(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
                Assert.Equal(new byte[20], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [Fact]
        public async Task ResponseBody_NoBufferWriteNoHeaders_DefaultsToChunked()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                context.Response.ShouldBuffer = false;
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
        public async Task ResponseBody_FlushThenBuffer_DefaultsToChunkedAndTerminates()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                context.Response.Body.Write(new byte[10], 0, 10);
                context.Response.Body.Flush();
                await context.Response.Body.WriteAsync(new byte[10], 0, 10);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.HasValue);
                Assert.Equal(20, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [Fact]
        public async Task ResponseBody_WriteChunked_ManuallyChunked()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                context.Response.Headers["transfeR-Encoding"] = "CHunked";
                Stream stream = context.Response.Body;
                var responseBytes = Encoding.ASCII.GetBytes("10\r\nManually Chunked\r\n0\r\n\r\n");
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.Equal("Manually Chunked", await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async Task ResponseBody_WriteContentLength_PassedThrough()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                context.Response.Headers["Content-lenGth"] = " 30 ";
                Stream stream = context.Response.Body;
#if DNX451
                stream.EndWrite(stream.BeginWrite(new byte[10], 0, 10, null, null));
#else
                await stream.WriteAsync(new byte[10], 0, 10);
#endif
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

        [Fact]
        public async Task ResponseBody_WriteContentLengthNoneWritten_Aborts()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                context.Response.Headers["Content-lenGth"] = " 20 ";
                context.Dispose();
#if !DNXCORE50
                // HttpClient retries the request because it didn't get a response.
                context = await server.GetContextAsync();
                context.Response.Headers["Content-lenGth"] = " 20 ";
                context.Dispose();
#endif
                await Assert.ThrowsAsync<HttpRequestException>(() => responseTask);
            }
        }

        [Fact]
        public async Task ResponseBody_WriteContentLengthNotEnoughWritten_Aborts()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                context.Response.Headers["Content-lenGth"] = " 20 ";
                context.Response.Body.Write(new byte[5], 0, 5);
                context.Dispose();
#if !DNXCORE50
                // HttpClient retries the request because it didn't get a response.
                context = await server.GetContextAsync();
                context.Response.Headers["Content-lenGth"] = " 20 ";
                context.Response.Body.Write(new byte[5], 0, 5);
                context.Dispose();
#endif
                await Assert.ThrowsAsync<HttpRequestException>(() => responseTask);
            }
        }

        [Fact]
        public async Task ResponseBody_WriteContentLengthTooMuchWritten_Throws()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                context.Response.Headers["Content-lenGth"] = " 10 ";
                context.Response.Body.Write(new byte[5], 0, 5);
                Assert.Throws<InvalidOperationException>(() => context.Response.Body.Write(new byte[6], 0, 6));
                context.Dispose();
#if !DNXCORE50
                // HttpClient retries the request because it didn't get a response.
                context = await server.GetContextAsync();
                context.Response.Headers["Content-lenGth"] = " 10 ";
                context.Response.Body.Write(new byte[5], 0, 5);
                Assert.Throws<InvalidOperationException>(() => context.Response.Body.Write(new byte[6], 0, 6));
                context.Dispose();
#endif
                await Assert.ThrowsAsync<HttpRequestException>(() => responseTask);
            }
        }

        [Fact]
        public async Task ResponseBody_WriteContentLengthExtraWritten_Throws()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                context.Response.Headers["Content-lenGth"] = " 10 ";
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

        [Fact]
        public async Task ResponseBody_WriteZeroCount_StartsResponse()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                context.Response.Body.Write(new byte[10], 0, 0);
                Assert.True(context.Response.HasStarted);
                await context.Response.Body.WriteAsync(new byte[10], 0, 0);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> ignored;
                Assert.True(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.False(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [Fact]
        public async Task ResponseBody_WriteMoreThanBufferLimitBufferWithNoHeaders_DefaultsToChunkedAndFlushes()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                context.Response.ShouldBuffer = true;
                for (int i = 0; i < 4; i++)
                {
                    context.Response.Body.Write(new byte[1020], 0, 1020);
                    Assert.True(context.Response.HasStarted);
                    Assert.False(context.Response.HasStartedSending);
                }
                context.Response.Body.Write(new byte[1020], 0, 1020);
                Assert.True(context.Response.HasStartedSending);
                context.Response.Body.Write(new byte[1020], 0, 1020);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.Equal(new byte[1020*6], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [Fact]
        public async Task ResponseBody_WriteAsyncMoreThanBufferLimitBufferWithNoHeaders_DefaultsToChunkedAndFlushes()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                context.Response.ShouldBuffer = true;
                for (int i = 0; i < 4; i++)
                {
                    await context.Response.Body.WriteAsync(new byte[1020], 0, 1020);
                    Assert.True(context.Response.HasStarted);
                    Assert.False(context.Response.HasStartedSending);
                }
                await context.Response.Body.WriteAsync(new byte[1020], 0, 1020);
                Assert.True(context.Response.HasStartedSending);
                await context.Response.Body.WriteAsync(new byte[1020], 0, 1020);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.Equal(new byte[1020 * 6], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [Fact]
        public async Task ResponseBody_WriteAsyncWithActiveCancellationToken_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                var cts = new CancellationTokenSource();
                // First write sends headers
                await context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                await context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new byte[20], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [Fact]
        public async Task ResponseBody_WriteAsyncWithTimerCancellationToken_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(1));
                // First write sends headers
                await context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                await context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new byte[20], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [Fact]
        public async Task ResponseBody_FirstWriteAsyncWithCanceledCancellationToken_CancelsButDoesNotAbort()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                var cts = new CancellationTokenSource();
                cts.Cancel();
                // First write sends headers
                var writeTask = context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                Assert.True(writeTask.IsCanceled);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [Fact]
        public async Task ResponseBody_SecondWriteAsyncWithCanceledCancellationToken_CancelsButDoesNotAbort()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.GetContextAsync();
                var cts = new CancellationTokenSource();
                // First write sends headers
                await context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                cts.Cancel();
                var writeTask = context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                Assert.True(writeTask.IsCanceled);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
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