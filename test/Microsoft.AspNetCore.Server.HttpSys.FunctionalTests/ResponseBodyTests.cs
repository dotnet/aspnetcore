// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

namespace Microsoft.AspNetCore.Server.HttpSys
{
    public class ResponseBodyTests
    {
        [ConditionalFact]
        public async Task ResponseBody_WriteNoHeaders_SetsChunked()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.Body.Write(new byte[10], 0, 10);
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
                Assert.Equal(new byte[20], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_WriteNoHeadersAndFlush_DefaultsToChunked()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, async httpContext =>
            {
                httpContext.Response.Body.Write(new byte[10], 0, 10);
                await httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
                await httpContext.Response.Body.FlushAsync();
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.Equal(new byte[20], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_WriteChunked_ManuallyChunked()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, async httpContext =>
            {
                httpContext.Response.Headers["transfeR-Encoding"] = "CHunked";
                Stream stream = httpContext.Response.Body;
                var responseBytes = Encoding.ASCII.GetBytes("10\r\nManually Chunked\r\n0\r\n\r\n");
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.Equal("Manually Chunked", await response.Content.ReadAsStringAsync());
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_WriteContentLength_PassedThrough()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, async httpContext =>
            {
                httpContext.Response.Headers["Content-lenGth"] = " 30 ";
                Stream stream = httpContext.Response.Body;
#if NET452
                stream.EndWrite(stream.BeginWrite(new byte[10], 0, 10, null, null));
#elif NETCOREAPP2_0
                await stream.WriteAsync(new byte[10], 0, 10);
#else
#error Target framework needs to be updated
#endif
                stream.Write(new byte[10], 0, 10);
                await stream.WriteAsync(new byte[10], 0, 10);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> contentLength;
                Assert.True(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.Equal("30", contentLength.First());
                Assert.Null(response.Headers.TransferEncodingChunked);
                Assert.Equal(new byte[30], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_WriteContentLengthNoneWritten_Throws()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.Headers["Content-lenGth"] = " 20 ";
                return Task.FromResult(0);
            }))
            {
                await Assert.ThrowsAsync<HttpRequestException>(() => SendRequestAsync(address));
            }
        }

        [ConditionalFact]
        public void ResponseBody_WriteContentLengthNotEnoughWritten_Throws()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.Headers["Content-lenGth"] = " 20 ";
                httpContext.Response.Body.Write(new byte[5], 0, 5);
                return Task.FromResult(0);
            }))
            {
                Assert.Throws<AggregateException>(() => SendRequestAsync(address).Result);
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_WriteContentLengthTooMuchWritten_Throws()
        {
            var completed = false;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.Headers["Content-lenGth"] = " 10 ";
                httpContext.Response.Body.Write(new byte[5], 0, 5);
                Assert.Throws<InvalidOperationException>(() => httpContext.Response.Body.Write(new byte[6], 0, 6));
                completed = true;
                return Task.FromResult(0);
            }))
            {
                await Assert.ThrowsAsync<HttpRequestException>(() => SendRequestAsync(address));
                Assert.True(completed);
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_WriteContentLengthExtraWritten_Throws()
        {
            var waitHandle = new ManualResetEvent(false);
            bool? appThrew = null;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                try
                {
                    httpContext.Response.Headers["Content-lenGth"] = " 10 ";
                    httpContext.Response.Body.Write(new byte[10], 0, 10);
                    httpContext.Response.Body.Write(new byte[9], 0, 9);
                    appThrew = false;
                }
                catch (Exception)
                {
                    appThrew = true;
                }
                waitHandle.Set();
                return Task.FromResult(0);
            }))
            {
                // The full response is received.
                HttpResponseMessage response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> contentLength;
                Assert.True(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.Equal("10", contentLength.First());
                Assert.Null(response.Headers.TransferEncodingChunked);
                Assert.Equal(new byte[10], await response.Content.ReadAsByteArrayAsync());

                Assert.True(waitHandle.WaitOne(100));
                Assert.True(appThrew.HasValue, "appThrew.HasValue");
                Assert.True(appThrew.Value, "appThrew.Value");
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_Write_TriggersOnStarting()
        {
            var onStartingCalled = false;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.OnStarting(state =>
                {
                    onStartingCalled = true;
                    Assert.Same(state, httpContext);
                    return Task.FromResult(0);
                }, httpContext);
                httpContext.Response.Body.Write(new byte[10], 0, 10);
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.True(onStartingCalled);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
                Assert.Equal(new byte[10], await response.Content.ReadAsByteArrayAsync());
            }
        }
#if NET452
        [ConditionalFact]
        public async Task ResponseBody_BeginWrite_TriggersOnStarting()
        {
            var onStartingCalled = false;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.OnStarting(state =>
                {
                    onStartingCalled = true;
                    Assert.Same(state, httpContext);
                    return Task.FromResult(0);
                }, httpContext);
                httpContext.Response.Body.EndWrite(httpContext.Response.Body.BeginWrite(new byte[10], 0, 10, null, null));
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.True(onStartingCalled);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
                Assert.Equal(new byte[10], await response.Content.ReadAsByteArrayAsync());
            }
        }
#elif NETCOREAPP2_0
#else
#error Target framework needs to be updated
#endif
        [ConditionalFact]
        public async Task ResponseBody_WriteAsync_TriggersOnStarting()
        {
            var onStartingCalled = false;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.OnStarting(state =>
                {
                    onStartingCalled = true;
                    Assert.Same(state, httpContext);
                    return Task.FromResult(0);
                }, httpContext);
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.True(onStartingCalled);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
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
