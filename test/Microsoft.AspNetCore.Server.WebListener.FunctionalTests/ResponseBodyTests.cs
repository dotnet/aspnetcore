// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Server.WebListener
{
    public class ResponseBodyTests
    {
        [Fact]
        public async Task ResponseBody_WriteNoHeaders_BuffersAndSetsContentLength()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.Body.Write(new byte[10], 0, 10);
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> ignored;
                Assert.True(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.False(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
                Assert.Equal(new byte[20], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [Fact]
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
                HttpResponseMessage response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.Equal(new byte[20], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [Fact]
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
                HttpResponseMessage response = await SendRequestAsync(address);
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
            using (Utilities.CreateHttpServer(out address, async httpContext =>
            {
                httpContext.Response.Headers["Content-lenGth"] = " 30 ";
                Stream stream = httpContext.Response.Body;
#if NET451
                stream.EndWrite(stream.BeginWrite(new byte[10], 0, 10, null, null));
#else
                await stream.WriteAsync(new byte[10], 0, 10);
#endif
                stream.Write(new byte[10], 0, 10);
                await stream.WriteAsync(new byte[10], 0, 10);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(address);
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

        [Fact]
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

        [Fact]
        public async Task ResponseBody_WriteContentLengthTooMuchWritten_Throws()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.Headers["Content-lenGth"] = " 10 ";
                httpContext.Response.Body.Write(new byte[5], 0, 5);
                httpContext.Response.Body.Write(new byte[6], 0, 6);
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(500, (int)response.StatusCode);
            }
        }

        [Fact]
        public async Task ResponseBody_WriteContentLengthExtraWritten_Throws()
        {
            ManualResetEvent waitHandle = new ManualResetEvent(false);
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

        private async Task<HttpResponseMessage> SendRequestAsync(string uri)
        {
            using (HttpClient client = new HttpClient())
            {
                return await client.GetAsync(uri);
            }
        }
    }
}
