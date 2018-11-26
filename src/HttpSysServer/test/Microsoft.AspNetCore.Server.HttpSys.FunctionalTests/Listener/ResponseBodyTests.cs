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

namespace Microsoft.AspNetCore.Server.HttpSys.Listener
{
    public class ResponseBodyTests
    {
        [ConditionalFact]
        public async Task ResponseBody_SyncWriteEnabledByDefault_ThrowsWhenDisabled()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);

                Assert.True(context.AllowSynchronousIO);

                context.Response.Body.Flush();
                context.Response.Body.Write(new byte[10], 0, 10);
                context.Response.Body.Flush();

                context.AllowSynchronousIO = false;

                Assert.Throws<InvalidOperationException>(() => context.Response.Body.Flush());
                Assert.Throws<InvalidOperationException>(() => context.Response.Body.Write(new byte[10], 0, 10));
                Assert.Throws<InvalidOperationException>(() => context.Response.Body.Flush());

                await context.Response.Body.WriteAsync(new byte[10], 0, 10);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.Equal(new byte[20], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_FlushThenWrite_DefaultsToChunkedAndTerminates()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                server.Options.AllowSynchronousIO = true;
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Body.Write(new byte[10], 0, 10);
                context.Response.Body.Flush();
                await context.Response.Body.WriteAsync(new byte[10], 0, 10);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.HasValue);
                Assert.Equal(20, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_WriteZeroCount_StartsChunkedResponse()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                server.Options.AllowSynchronousIO = true;
                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Body.Write(new byte[10], 0, 0);
                Assert.True(context.Response.HasStarted);
                await context.Response.Body.WriteAsync(new byte[10], 0, 0);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_WriteAsyncWithActiveCancellationToken_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                var cts = new CancellationTokenSource();
                // First write sends headers
                await context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                await context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new byte[20], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_WriteAsyncWithTimerCancellationToken_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(10));
                // First write sends headers
                await context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                await context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new byte[20], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [ConditionalFact]
        public async Task ResponseBodyWriteExceptions_FirstWriteAsyncWithCanceledCancellationToken_CancelsAndAborts()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                server.Options.ThrowWriteExceptions = true;
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                var cts = new CancellationTokenSource();
                cts.Cancel();
                // First write sends headers
                var writeTask = context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                Assert.True(writeTask.IsCanceled);
                context.Dispose();

                await Assert.ThrowsAsync<HttpRequestException>(() => responseTask);
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_FirstWriteAsyncWithCanceledCancellationToken_CancelsAndAborts()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                var cts = new CancellationTokenSource();
                cts.Cancel();
                // First write sends headers
                var writeTask = context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                Assert.True(writeTask.IsCanceled);
                context.Dispose();

                await Assert.ThrowsAsync<HttpRequestException>(() => responseTask);
            }
        }

        [ConditionalFact]
        public async Task ResponseBodyWriteExceptions_SecondWriteAsyncWithCanceledCancellationToken_CancelsAndAborts()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                server.Options.ThrowWriteExceptions = true;
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                var cts = new CancellationTokenSource();
                // First write sends headers
                await context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                var response = await responseTask;
                cts.Cancel();
                var writeTask = context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                Assert.True(writeTask.IsCanceled);
                context.Dispose();

                await Assert.ThrowsAsync<HttpRequestException>(() => response.Content.LoadIntoBufferAsync());
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_SecondWriteAsyncWithCanceledCancellationToken_CancelsAndAborts()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                var cts = new CancellationTokenSource();
                // First write sends headers
                await context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                var response = await responseTask;
                cts.Cancel();
                var writeTask = context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                Assert.True(writeTask.IsCanceled);
                context.Dispose();

                await Assert.ThrowsAsync<HttpRequestException>(() => response.Content.LoadIntoBufferAsync());
            }
        }

        [ConditionalFact]
        public async Task ResponseBodyWriteExceptions_ClientDisconnectsBeforeFirstWrite_WriteThrows()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                server.Options.ThrowWriteExceptions = true;
                server.Options.AllowSynchronousIO = true;
                var cts = new CancellationTokenSource();
                var responseTask = SendRequestAsync(address, cts.Token);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                // First write sends headers
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => responseTask);
                Assert.True(context.DisconnectToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));
                Assert.Throws<IOException>(() =>
                {
                    // It can take several tries before Write notices the disconnect.
                    for (int i = 0; i < Utilities.WriteRetryLimit; i++)
                    {
                        context.Response.Body.Write(new byte[1000], 0, 1000);
                    }
                });

                Assert.Throws<ObjectDisposedException>(() => context.Response.Body.Write(new byte[1000], 0, 1000));

                context.Dispose();
            }
        }

        [ConditionalFact]
        public async Task ResponseBodyWriteExceptions_ClientDisconnectsBeforeFirstWriteAsync_WriteThrows()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                server.Options.ThrowWriteExceptions = true;
                var cts = new CancellationTokenSource();
                var responseTask = SendRequestAsync(address, cts.Token);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);

                // First write sends headers
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => responseTask);

                Assert.True(context.DisconnectToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));
                await Assert.ThrowsAsync<IOException>(async () =>
                {
                    // It can take several tries before Write notices the disconnect.
                    for (int i = 0; i < Utilities.WriteRetryLimit; i++)
                    {
                        await context.Response.Body.WriteAsync(new byte[1000], 0, 1000);
                    }
                });

                await Assert.ThrowsAsync<ObjectDisposedException>(() => context.Response.Body.WriteAsync(new byte[1000], 0, 1000));

                context.Dispose();
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_ClientDisconnectsBeforeFirstWrite_WriteCompletesSilently()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var cts = new CancellationTokenSource();
                var responseTask = SendRequestAsync(address, cts.Token);

                server.Options.AllowSynchronousIO = true;
                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                // First write sends headers
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => responseTask);
                Assert.True(context.DisconnectToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));
                // It can take several tries before Write notices the disconnect.
                for (int i = 0; i < Utilities.WriteRetryLimit; i++)
                {
                    context.Response.Body.Write(new byte[1000], 0, 1000);
                }
                context.Dispose();
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_ClientDisconnectsBeforeFirstWriteAsync_WriteCompletesSilently()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var cts = new CancellationTokenSource();
                var responseTask = SendRequestAsync(address, cts.Token);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                // First write sends headers
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => responseTask);
                Assert.True(context.DisconnectToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));
                // It can take several tries before Write notices the disconnect.
                for (int i = 0; i < Utilities.WriteRetryLimit; i++)
                {
                    await context.Response.Body.WriteAsync(new byte[1000], 0, 1000);
                }
                context.Dispose();
            }
        }

        [ConditionalFact]
        public async Task ResponseBodyWriteExceptions_ClientDisconnectsBeforeSecondWrite_WriteThrows()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                server.Options.ThrowWriteExceptions = true;
                RequestContext context;
                using (var client = new HttpClient())
                {
                    var responseTask = client.GetAsync(address, HttpCompletionOption.ResponseHeadersRead);

                    context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                    // First write sends headers
                    context.Response.Body.Write(new byte[10], 0, 10);

                    var response = await responseTask;
                    response.EnsureSuccessStatusCode();
                    response.Dispose();
                }

                Assert.True(context.DisconnectToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));
                Assert.Throws<IOException>(() =>
                {
                    // It can take several tries before Write notices the disconnect.
                    for (int i = 0; i < Utilities.WriteRetryLimit; i++)
                    {
                        context.Response.Body.Write(new byte[1000], 0, 1000);
                    }
                });
                context.Dispose();
            }
        }

        [ConditionalFact]
        public async Task ResponseBodyWriteExceptions_ClientDisconnectsBeforeSecondWriteAsync_WriteThrows()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                server.Options.ThrowWriteExceptions = true;
                RequestContext context;
                using (var client = new HttpClient())
                {
                    var responseTask = client.GetAsync(address, HttpCompletionOption.ResponseHeadersRead);

                    context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                    // First write sends headers
                    await context.Response.Body.WriteAsync(new byte[10], 0, 10);

                    var response = await responseTask;
                    response.EnsureSuccessStatusCode();
                    response.Dispose();
                }

                Assert.True(context.DisconnectToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));
                await Assert.ThrowsAsync<IOException>(async () =>
                {
                    // It can take several tries before Write notices the disconnect.
                    for (int i = 0; i < Utilities.WriteRetryLimit; i++)
                    {
                        await context.Response.Body.WriteAsync(new byte[1000], 0, 1000);
                    }
                });
                context.Dispose();
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_ClientDisconnectsBeforeSecondWrite_WriteCompletesSilently()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                server.Options.AllowSynchronousIO = true;
                RequestContext context;
                using (var client = new HttpClient())
                {
                    var responseTask = client.GetAsync(address, HttpCompletionOption.ResponseHeadersRead);

                    context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                    // First write sends headers
                    context.Response.Body.Write(new byte[10], 0, 10);

                    var response = await responseTask;
                    response.EnsureSuccessStatusCode();
                    response.Dispose();
                }

                Assert.True(context.DisconnectToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));
                // It can take several tries before Write notices the disconnect.
                for (int i = 0; i < Utilities.WriteRetryLimit; i++)
                {
                    context.Response.Body.Write(new byte[1000], 0, 1000);
                }
                context.Dispose();
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_ClientDisconnectsBeforeSecondWriteAsync_WriteCompletesSilently()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                RequestContext context;
                using (var client = new HttpClient())
                {
                    var responseTask = client.GetAsync(address, HttpCompletionOption.ResponseHeadersRead);

                    context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                    // First write sends headers
                    await context.Response.Body.WriteAsync(new byte[10], 0, 10);

                    var response = await responseTask;
                    response.EnsureSuccessStatusCode();
                    response.Dispose();
                }

                Assert.True(context.DisconnectToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));
                // It can take several tries before Write notices the disconnect.
                for (int i = 0; i < Utilities.WriteRetryLimit; i++)
                {
                    await context.Response.Body.WriteAsync(new byte[1000], 0, 1000);
                }
                context.Dispose();
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string uri, CancellationToken cancellationToken = new CancellationToken())
        {
            using (HttpClient client = new HttpClient() { Timeout = Utilities.DefaultTimeout })
            {
                return await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
        }
    }
}