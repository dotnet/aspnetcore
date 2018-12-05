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

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);

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
        public async Task ResponseBody_WriteNoHeaders_DefaultsToChunked()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                server.Options.AllowSynchronousIO = true;
                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                context.Response.Body.Write(new byte[10], 0, 10);
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

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
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
        public async Task ResponseBody_WriteChunked_ManuallyChunked()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                context.Response.Headers["transfeR-Encoding"] = "CHunked";
                Stream stream = context.Response.Body;
                var responseBytes = Encoding.ASCII.GetBytes("10\r\nManually Chunked\r\n0\r\n\r\n");
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                context.Dispose();

                var response = await responseTask;
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
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                server.Options.AllowSynchronousIO = true;
                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                context.Response.Headers["Content-lenGth"] = " 30 ";
                var stream = context.Response.Body;
                stream.EndWrite(stream.BeginWrite(new byte[10], 0, 10, null, null));
                stream.Write(new byte[10], 0, 10);
                await stream.WriteAsync(new byte[10], 0, 10);
                context.Dispose();

                var response = await responseTask;
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
        public async Task ResponseBody_WriteContentLengthNoneWritten_Aborts()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                context.Response.Headers["Content-lenGth"] = " 20 ";
                context.Dispose();
#if NET461
                // HttpClient retries the request because it didn't get a response.
                context = await server.AcceptAsync(Utilities.DefaultTimeout);
                context.Response.Headers["Content-lenGth"] = " 20 ";
                context.Dispose();
#elif NETCOREAPP2_0 || NETCOREAPP2_1
#else
#error Target framework needs to be updated
#endif
                await Assert.ThrowsAsync<HttpRequestException>(() => responseTask);
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_WriteContentLengthNotEnoughWritten_Aborts()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                context.Response.Headers["Content-lenGth"] = " 20 ";
                context.Response.Body.Write(new byte[5], 0, 5);
                context.Dispose();

                await Assert.ThrowsAsync<HttpRequestException>(() => responseTask);
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_WriteContentLengthTooMuchWritten_Throws()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                context.Response.Headers["Content-lenGth"] = " 10 ";
                context.Response.Body.Write(new byte[5], 0, 5);
                Assert.Throws<InvalidOperationException>(() => context.Response.Body.Write(new byte[6], 0, 6));
                context.Dispose();

                await Assert.ThrowsAsync<HttpRequestException>(() => responseTask);
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_WriteContentLengthExtraWritten_Throws()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                server.Options.AllowSynchronousIO = true;
                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
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

        [ConditionalFact]
        public async Task ResponseBody_WriteZeroCount_StartsChunkedResponse()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                server.Options.AllowSynchronousIO = true;
                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
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

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
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

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
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

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                var cts = new CancellationTokenSource();
                cts.Cancel();
                // First write sends headers
                var writeTask = context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                Assert.True(writeTask.IsCanceled);
                context.Dispose();
#if NET461
                // HttpClient retries the request because it didn't get a response.
                context = await server.AcceptAsync(Utilities.DefaultTimeout);
                cts = new CancellationTokenSource();
                cts.Cancel();
                // First write sends headers
                writeTask = context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                Assert.True(writeTask.IsCanceled);
                context.Dispose();
#elif NETCOREAPP2_0 || NETCOREAPP2_1
#else
#error Target framework needs to be updated
#endif
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

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                var cts = new CancellationTokenSource();
                cts.Cancel();
                // First write sends headers
                var writeTask = context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                Assert.True(writeTask.IsCanceled);
                context.Dispose();
#if NET461
                // HttpClient retries the request because it didn't get a response.
                context = await server.AcceptAsync(Utilities.DefaultTimeout);
                cts = new CancellationTokenSource();
                cts.Cancel();
                // First write sends headers
                writeTask = context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                Assert.True(writeTask.IsCanceled);
                context.Dispose();
#elif NETCOREAPP2_0 || NETCOREAPP2_1
#else
#error Target framework needs to be updated
#endif
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

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                var cts = new CancellationTokenSource();
                // First write sends headers
                await context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                cts.Cancel();
                var writeTask = context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                Assert.True(writeTask.IsCanceled);
                context.Dispose();

                await Assert.ThrowsAsync<HttpRequestException>(() => responseTask);
            }
        }

        [ConditionalFact]
        public async Task ResponseBody_SecondWriteAsyncWithCanceledCancellationToken_CancelsAndAborts()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                var cts = new CancellationTokenSource();
                // First write sends headers
                await context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                cts.Cancel();
                var writeTask = context.Response.Body.WriteAsync(new byte[10], 0, 10, cts.Token);
                Assert.True(writeTask.IsCanceled);
                context.Dispose();

                await Assert.ThrowsAsync<HttpRequestException>(() => responseTask);
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

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
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

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);

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
                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
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

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
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

                    context = await server.AcceptAsync(Utilities.DefaultTimeout);
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

                    context = await server.AcceptAsync(Utilities.DefaultTimeout);
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

                    context = await server.AcceptAsync(Utilities.DefaultTimeout);
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

                    context = await server.AcceptAsync(Utilities.DefaultTimeout);
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
            using (HttpClient client = new HttpClient())
            {
                return await client.GetAsync(uri, cancellationToken);
            }
        }
    }
}