// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.Listener
{
    public class ResponseSendFileTests
    {
        private readonly string AbsoluteFilePath;
        private readonly string RelativeFilePath;
        private readonly long FileLength;

        public ResponseSendFileTests()
        {
            AbsoluteFilePath = Directory.GetFiles(Directory.GetCurrentDirectory()).First();
            RelativeFilePath = Path.GetFileName(AbsoluteFilePath);
            FileLength = new FileInfo(AbsoluteFilePath).Length;
        }

        [ConditionalFact]
        public async Task ResponseSendFile_MissingFile_Throws()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                await Assert.ThrowsAsync<FileNotFoundException>(() =>
                    context.Response.SendFileAsync("Missing.txt", 0, null, CancellationToken.None));
                context.Dispose();

                var response = await responseTask;
                response.EnsureSuccessStatusCode();
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_NoHeaders_DefaultsToChunked()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.Equal(FileLength, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_RelativeFile_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                await context.Response.SendFileAsync(RelativeFilePath, 0, null, CancellationToken.None);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.Equal(FileLength, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_Unspecified_Chunked()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value);
                Assert.Equal(FileLength, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_MultipleWrites_Chunked()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value);
                Assert.Equal(FileLength * 2, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_HalfOfFile_Chunked()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, FileLength / 2, CancellationToken.None);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value);
                Assert.Equal(FileLength / 2, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_OffsetOutOfRange_Throws()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                    () => context.Response.SendFileAsync(AbsoluteFilePath, 1234567, null, CancellationToken.None));
                context.Dispose();

                var response = await responseTask;
                response.EnsureSuccessStatusCode();
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_CountOutOfRange_Throws()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                    () => context.Response.SendFileAsync(AbsoluteFilePath, 0, 1234567, CancellationToken.None));
                context.Dispose();

                var response = await responseTask;
                response.EnsureSuccessStatusCode();
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_Count0_Chunked()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, 0, CancellationToken.None);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value);
                Assert.Empty((await response.Content.ReadAsByteArrayAsync()));
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_EmptyFileCountUnspecified_SetsChunkedAndFlushesHeaders()
        {
            var emptyFilePath = Path.Combine(Directory.GetCurrentDirectory(), "zz_" + Guid.NewGuid().ToString() + "EmptyTestFile.txt");
            var emptyFile = File.Create(emptyFilePath, 1024);
            emptyFile.Dispose();

            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                await context.Response.SendFileAsync(emptyFilePath, 0, null, CancellationToken.None);
                Assert.True(context.Response.HasStarted);
                await context.Response.Body.WriteAsync(new byte[10], 0, 10, CancellationToken.None);
                context.Dispose();
                File.Delete(emptyFilePath);

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.HasValue);
                Assert.Equal(10, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_ContentLength_PassedThrough()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                context.Response.Headers["Content-lenGth"] = FileLength.ToString();
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.True(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.Equal(FileLength.ToString(), contentLength.First());
                Assert.Null(response.Headers.TransferEncodingChunked);
                Assert.Equal(FileLength, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_ContentLengthSpecific_PassedThrough()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                context.Response.Headers["Content-lenGth"] = "10";
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, 10, CancellationToken.None);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.True(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.Equal("10", contentLength.First());
                Assert.Null(response.Headers.TransferEncodingChunked);
                Assert.Equal(10, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_ContentLength0_PassedThrough()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                context.Response.Headers["Content-lenGth"] = "0";
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, 0, CancellationToken.None);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.True(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.Equal("0", contentLength.First());
                Assert.Null(response.Headers.TransferEncodingChunked);
                Assert.Empty((await response.Content.ReadAsByteArrayAsync()));
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_WithActiveCancellationToken_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                var cts = new CancellationTokenSource();
                // First write sends headers
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(FileLength * 2, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_WithTimerCancellationToken_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(10));
                // First write sends headers
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(FileLength * 2, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFileWriteExceptions_FirstCallWithCanceledCancellationToken_CancelsAndAborts()
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
                var writeTask = context.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
                Assert.True(writeTask.IsCanceled);
                context.Dispose();
#if NET461
                // .NET HttpClient automatically retries a request if it does not get a response.
                context = await server.AcceptAsync(Utilities.DefaultTimeout);
                cts = new CancellationTokenSource();
                cts.Cancel();
                // First write sends headers
                writeTask = context.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
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
        public async Task ResponseSendFile_FirstSendWithCanceledCancellationToken_CancelsAndAborts()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                var cts = new CancellationTokenSource();
                cts.Cancel();
                // First write sends headers
                var writeTask = context.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
                Assert.True(writeTask.IsCanceled);
                context.Dispose();
#if NET461
                // .NET HttpClient automatically retries a request if it does not get a response.
                context = await server.AcceptAsync(Utilities.DefaultTimeout);
                cts = new CancellationTokenSource();
                cts.Cancel();
                // First write sends headers
                writeTask = context.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
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
        public async Task ResponseSendFileExceptions_SecondSendWithCanceledCancellationToken_CancelsAndAborts()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                server.Options.ThrowWriteExceptions = true;
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                var cts = new CancellationTokenSource();
                // First write sends headers
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
                cts.Cancel();
                var writeTask = context.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
                Assert.True(writeTask.IsCanceled);
                context.Dispose();

                await Assert.ThrowsAsync<HttpRequestException>(() => responseTask);
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_SecondSendWithCanceledCancellationToken_CancelsAndAborts()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                var cts = new CancellationTokenSource();
                // First write sends headers
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
                cts.Cancel();
                var writeTask = context.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
                Assert.True(writeTask.IsCanceled);
                context.Dispose();

                await Assert.ThrowsAsync<HttpRequestException>(() => responseTask);
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFileExceptions_ClientDisconnectsBeforeFirstSend_SendThrows()
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
                    // It can take several tries before Send notices the disconnect.
                    for (int i = 0; i < Utilities.WriteRetryLimit; i++)
                    {
                        await context.Response.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);
                    }
                });

                await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                    context.Response.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None));

                context.Dispose();
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_ClientDisconnectsBeforeFirstSend_SendCompletesSilently()
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
                // It can take several tries before Send notices the disconnect.
                for (int i = 0; i < Utilities.WriteRetryLimit; i++)
                {
                    await context.Response.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);
                }
                context.Dispose();
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFileExceptions_ClientDisconnectsBeforeSecondSend_SendThrows()
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
                    var sendFileTask = context.Response.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);

                    var response = await responseTask;
                    response.EnsureSuccessStatusCode();
                    // Drain data from the connection so that SendFileAsync can complete.
                    var bufferTask = response.Content.LoadIntoBufferAsync();

                    await sendFileTask;
                    response.Dispose();
                }

                Assert.True(context.DisconnectToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));
                await Assert.ThrowsAsync<IOException>(async () =>
                {
                    // It can take several tries before Write notices the disconnect.
                    for (int i = 0; i < Utilities.WriteRetryLimit; i++)
                    {
                        await context.Response.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);
                    }
                });
                context.Dispose();
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_ClientDisconnectsBeforeSecondSend_SendCompletesSilently()
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
                    var sendFileTask = context.Response.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);

                    var response = await responseTask;
                    response.EnsureSuccessStatusCode();
                    // Drain data from the connection so that SendFileAsync can complete.
                    var bufferTask = response.Content.LoadIntoBufferAsync();

                    await sendFileTask;
                    response.Dispose();
                }

                Assert.True(context.DisconnectToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));
                // It can take several tries before Write notices the disconnect.
                for (int i = 0; i < Utilities.WriteRetryLimit; i++)
                {
                    await context.Response.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);
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