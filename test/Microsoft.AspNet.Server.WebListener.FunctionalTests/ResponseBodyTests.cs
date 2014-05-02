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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.PipelineCore;
using Xunit;

namespace Microsoft.AspNet.Server.WebListener
{
    public class ResponseBodyTests
    {
        private const string Address = "http://localhost:8080/";

        [Fact]
        public async Task ResponseBody_WriteNoHeaders_DefaultsToChunked()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                httpContext.Response.Body.Write(new byte[10], 0, 10);
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
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
            using (Utilities.CreateHttpServer(env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                httpContext.Request.Headers["transfeR-Encoding"] = " CHunked ";
                Stream stream = httpContext.Response.Body;
                stream.EndWrite(stream.BeginWrite(new byte[10], 0, 10, null, null));
                stream.Write(new byte[10], 0, 10);
                return stream.WriteAsync(new byte[10], 0, 10);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
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
            using (Utilities.CreateHttpServer(env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                httpContext.Response.Headers["Content-lenGth"] = " 30 ";
                Stream stream = httpContext.Response.Body;
                stream.EndWrite(stream.BeginWrite(new byte[10], 0, 10, null, null));
                stream.Write(new byte[10], 0, 10);
                return stream.WriteAsync(new byte[10], 0, 10);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
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
        [Fact]
        public async Task ResponseBody_WriteContentLengthNoneWritten_Throws()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                httpContext.Response.Headers["Content-lenGth"] = " 20 ";
                return Task.FromResult(0);
            }))
            {
                await Assert.ThrowsAsync<HttpRequestException>(() => SendRequestAsync(Address));
            }
        }

        [Fact]
        public void ResponseBody_WriteContentLengthNotEnoughWritten_Throws()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                httpContext.Response.Headers["Content-lenGth"] = " 20 ";
                httpContext.Response.Body.Write(new byte[5], 0, 5);
                return Task.FromResult(0);
            }))
            {
                Assert.Throws<AggregateException>(() => SendRequestAsync(Address).Result);
            }
        }

        [Fact]
        public void ResponseBody_WriteContentLengthTooMuchWritten_Throws()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                httpContext.Response.Headers["Content-lenGth"] = " 10 ";
                httpContext.Response.Body.Write(new byte[5], 0, 5);
                httpContext.Response.Body.Write(new byte[6], 0, 6);
                return Task.FromResult(0);
            }))
            {
                Assert.Throws<AggregateException>(() => SendRequestAsync(Address).Result);
            }
        }

        [Fact]
        public async Task ResponseBody_WriteContentLengthExtraWritten_Throws()
        {
            ManualResetEvent waitHandle = new ManualResetEvent(false);
            bool? appThrew = null;
            using (Utilities.CreateHttpServer(env =>
            {
                try
                {
                    var httpContext = new DefaultHttpContext((IFeatureCollection)env);
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
                HttpResponseMessage response = await SendRequestAsync(Address);
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
