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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    public class ResponseBodyTests
    {
        [ConditionalFact]
        public async Task ResponseBody_StartAsync_LocksHeadersAndTriggersOnStarting()
        {
            using (Utilities.CreateHttpServer(out var address, async httpContext =>
            {
                var startingTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                httpContext.Response.OnStarting(() =>
                {
                    startingTcs.SetResult(0);
                    return Task.CompletedTask;
                });
                await httpContext.Response.StartAsync();
                Assert.True(httpContext.Response.HasStarted);
                Assert.True(httpContext.Response.Headers.IsReadOnly);
                await startingTcs.Task.WithTimeout();
                await httpContext.Response.WriteAsync("Hello World");
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
                Assert.Equal("Hello World", await response.Content.ReadAsStringAsync());
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_CompleteAsync_TriggersOnStartingAndLocksHeaders()
        {
            var responseReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (Utilities.CreateHttpServer(out var address, async httpContext =>
            {
                var startingTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                httpContext.Response.OnStarting(() =>
                {
                    startingTcs.SetResult(0);
                    return Task.CompletedTask;
                });
                await httpContext.Response.CompleteAsync();
                Assert.True(httpContext.Response.HasStarted);
                Assert.True(httpContext.Response.Headers.IsReadOnly);
                await startingTcs.Task.WithTimeout();
                await responseReceived.Task.WithTimeout();
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.Equal(0, response.Content.Headers.ContentLength);
                responseReceived.SetResult(0);
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_CompleteAsync_FlushesThePipe()
        {
            var responseReceived = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (Utilities.CreateHttpServer(out var address, async httpContext =>
            {
                var writer = httpContext.Response.BodyWriter;
                var memory = writer.GetMemory();
                writer.Advance(memory.Length);
                await httpContext.Response.CompleteAsync();
                await responseReceived.Task.WithTimeout();
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.True(0 < (await response.Content.ReadAsByteArrayAsync()).Length);
                responseReceived.SetResult(0);
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_PipeAdapter_AutomaticallyFlushed()
        {
            using (Utilities.CreateHttpServer(out var address, httpContext =>
            {
                var writer = httpContext.Response.BodyWriter;
                var memory = writer.GetMemory();
                writer.Advance(memory.Length);
                return Task.CompletedTask;
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.True(0 < (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_WriteNoHeaders_SetsChunked()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
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
                httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
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
                httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
                httpContext.Response.Headers["Content-lenGth"] = " 30 ";
                Stream stream = httpContext.Response.Body;
                stream.EndWrite(stream.BeginWrite(new byte[10], 0, 10, null, null));
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
        public async Task ResponseBody_WriteContentLengthNotEnoughWritten_Throws()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.Headers["Content-lenGth"] = " 20 ";
                return httpContext.Response.Body.WriteAsync(new byte[5], 0, 5);
            }))
            {
                await Assert.ThrowsAsync<HttpRequestException>(async () => await SendRequestAsync(address));
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_WriteContentLengthTooMuchWritten_Throws()
        {
            var completed = false;
            string address;
            using (Utilities.CreateHttpServer(out address, async httpContext =>
            {
                httpContext.Response.Headers["Content-lenGth"] = " 10 ";
                await httpContext.Response.Body.WriteAsync(new byte[5], 0, 5);
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    httpContext.Response.Body.WriteAsync(new byte[6], 0, 6));
                completed = true;
            }))
            {
                await Assert.ThrowsAsync<HttpRequestException>(() => SendRequestAsync(address));
                Assert.True(completed);
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_WriteContentLengthExtraWritten_Throws()
        {
            var requestThrew = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (Utilities.CreateHttpServer(out var address, httpContext =>
            {
                try
                {
                    httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
                    httpContext.Response.Headers["Content-lenGth"] = " 10 ";
                    httpContext.Response.Body.Write(new byte[10], 0, 10);
                    httpContext.Response.Body.Write(new byte[9], 0, 9);
                    requestThrew.SetResult(false);
                }
                catch (Exception)
                {
                    requestThrew.SetResult(true);
                }
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

                Assert.True(await requestThrew.Task.TimeoutAfter(TimeSpan.FromSeconds(10)));
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_Write_TriggersOnStarting()
        {
            var onStartingCalled = false;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
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
