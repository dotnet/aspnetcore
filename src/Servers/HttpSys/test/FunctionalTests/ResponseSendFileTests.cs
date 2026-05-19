// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys;

public class ResponseSendFileTests : LoggedTest
{
    private readonly string AbsoluteFilePath;
    private readonly string RelativeFilePath;
    private readonly long FileLength;

    public ResponseSendFileTests()
    {
        AbsoluteFilePath = Directory.GetFiles(Directory.GetCurrentDirectory()).First();
        RelativeFilePath = Path.GetFileName(AbsoluteFilePath);
        FileLength = new FileInfo(AbsoluteFilePath).Length;
        Assert.True(FileLength > 0, "FileLength is 0");
    }

    [ConditionalFact]
    public async Task ResponseSendFile_MissingFile_Throws()
    {
        var appThrew = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (Utilities.CreateHttpServer(out var address, async httpContext =>
        {
            var sendFile = httpContext.Features.Get<IHttpResponseBodyFeature>();
            try
            {
                await sendFile.SendFileAsync(string.Empty, 0, null, CancellationToken.None);
                appThrew.SetResult(false);
            }
            catch (Exception)
            {
                appThrew.SetResult(true);
                throw;
            }
        }, LoggerFactory))
        {
            HttpResponseMessage response = await SendRequestAsync(address);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.True(await appThrew.Task.TimeoutAfter(TimeSpan.FromSeconds(10)));
        }
    }

    [ConditionalFact]
    public async Task ResponseSendFile_NoHeaders_DefaultsToChunked()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var sendFile = httpContext.Features.Get<IHttpResponseBodyFeature>();
            return sendFile.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);
        }, LoggerFactory))
        {
            HttpResponseMessage response = await SendRequestAsync(address);
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
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var sendFile = httpContext.Features.Get<IHttpResponseBodyFeature>();
            return sendFile.SendFileAsync(RelativeFilePath, 0, null, CancellationToken.None);
        }, LoggerFactory))
        {
            HttpResponseMessage response = await SendRequestAsync(address);
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
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var sendFile = httpContext.Features.Get<IHttpResponseBodyFeature>();
            return sendFile.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
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
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var sendFile = httpContext.Features.Get<IHttpResponseBodyFeature>();
            sendFile.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None).Wait();
            return sendFile.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
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
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var sendFile = httpContext.Features.Get<IHttpResponseBodyFeature>();
            return sendFile.SendFileAsync(AbsoluteFilePath, 0, FileLength / 2, CancellationToken.None);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
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
        var completed = false;
        string address;
        using (Utilities.CreateHttpServer(out address, async httpContext =>
        {
            var sendFile = httpContext.Features.Get<IHttpResponseBodyFeature>();
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                sendFile.SendFileAsync(AbsoluteFilePath, 1234567, null, CancellationToken.None));
            completed = true;
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            response.EnsureSuccessStatusCode();
            Assert.True(completed);
        }
    }

    [ConditionalFact]
    public async Task ResponseSendFile_CountOutOfRange_Throws()
    {
        var completed = false;
        string address;
        using (Utilities.CreateHttpServer(out address, async httpContext =>
        {
            var sendFile = httpContext.Features.Get<IHttpResponseBodyFeature>();
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                sendFile.SendFileAsync(AbsoluteFilePath, 0, 1234567, CancellationToken.None));
            completed = true;
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            response.EnsureSuccessStatusCode();
            Assert.True(completed);
        }
    }

    [ConditionalFact]
    public async Task ResponseSendFile_Count0_Chunked()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var sendFile = httpContext.Features.Get<IHttpResponseBodyFeature>();
            return sendFile.SendFileAsync(AbsoluteFilePath, 0, 0, CancellationToken.None);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(200, (int)response.StatusCode);
            IEnumerable<string> contentLength;
            Assert.False(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
            Assert.True(response.Headers.TransferEncodingChunked.Value);
            Assert.Empty((await response.Content.ReadAsByteArrayAsync()));
        }
    }

    [ConditionalFact]
    public async Task ResponseSendFile_ContentLength_PassedThrough()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var sendFile = httpContext.Features.Get<IHttpResponseBodyFeature>();
            httpContext.Response.Headers["Content-lenGth"] = FileLength.ToString(CultureInfo.InvariantCulture);
            return sendFile.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(200, (int)response.StatusCode);
            IEnumerable<string> contentLength;
            Assert.True(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
            Assert.Equal(FileLength.ToString(CultureInfo.InvariantCulture), contentLength.First());
            Assert.Null(response.Headers.TransferEncodingChunked);
            Assert.Equal(FileLength, (await response.Content.ReadAsByteArrayAsync()).Length);
        }
    }

    [ConditionalFact]
    public async Task ResponseSendFile_ContentLengthSpecific_PassedThrough()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var sendFile = httpContext.Features.Get<IHttpResponseBodyFeature>();
            httpContext.Response.Headers["Content-lenGth"] = "10";
            return sendFile.SendFileAsync(AbsoluteFilePath, 0, 10, CancellationToken.None);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
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
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var sendFile = httpContext.Features.Get<IHttpResponseBodyFeature>();
            httpContext.Response.Headers["Content-lenGth"] = "0";
            return sendFile.SendFileAsync(AbsoluteFilePath, 0, 0, CancellationToken.None);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(200, (int)response.StatusCode);
            IEnumerable<string> contentLength;
            Assert.True(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
            Assert.Equal("0", contentLength.First());
            Assert.Null(response.Headers.TransferEncodingChunked);
            Assert.Empty((await response.Content.ReadAsByteArrayAsync()));
        }
    }

    [ConditionalFact]
    public async Task ResponseSendFile_TriggersOnStarting()
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
            var sendFile = httpContext.Features.Get<IHttpResponseBodyFeature>();
            return sendFile.SendFileAsync(AbsoluteFilePath, 0, 10, CancellationToken.None);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal(new Version(1, 1), response.Version);
            Assert.True(onStartingCalled);
            IEnumerable<string> ignored;
            Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
            Assert.True(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
            Assert.Equal(10, (await response.Content.ReadAsByteArrayAsync()).Length);
        }
    }

    [ConditionalFact]
    public async Task ResponseSendFile_EmptyFileCountUnspecified_SetsChunkedAndFlushesHeaders()
    {
        var emptyFilePath = Path.Combine(Directory.GetCurrentDirectory(), "zz_" + Guid.NewGuid().ToString() + "EmptyTestFile.txt");
        var emptyFile = File.Create(emptyFilePath, 1024);
        emptyFile.Dispose();
        try
        {
            using (Utilities.CreateHttpServer(out var address, async httpContext =>
            {
                await httpContext.Response.SendFileAsync(emptyFilePath, 0, null, CancellationToken.None);
                Assert.True(httpContext.Response.HasStarted);
                await httpContext.Response.Body.WriteAsync(new byte[10], 0, 10, CancellationToken.None);
            }, LoggerFactory))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.False(response.Content.Headers.TryGetValues("content-length", out var contentLength), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.HasValue);
                Assert.Equal(10, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }
        finally
        {
            File.Delete(emptyFilePath);
        }
    }

    [ConditionalFact]
    public async Task ResponseSendFile_WithActiveCancellationToken_Success()
    {
        using (Utilities.CreateHttpServer(out var address, async httpContext =>
        {
            var cts = new CancellationTokenSource();
            // First write sends headers
            await httpContext.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
            await httpContext.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal(FileLength * 2, (await response.Content.ReadAsByteArrayAsync()).Length);
        }
    }

    [ConditionalFact]
    public async Task ResponseSendFile_WithTimerCancellationToken_Success()
    {
        using (Utilities.CreateHttpServer(out var address, async httpContext =>
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            // First write sends headers
            await httpContext.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
            await httpContext.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal(FileLength * 2, (await response.Content.ReadAsByteArrayAsync()).Length);
        }
    }

    [ConditionalFact]
    public async Task ResponseSendFileWriteExceptions_FirstCallWithCanceledCancellationToken_CancelsAndAborts()
    {
        var testComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (Utilities.CreateHttpServer(out var address, httpContext =>
        {
            try
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                // First write sends headers
                var writeTask = httpContext.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
                Assert.True(writeTask.IsCanceled);
                testComplete.SetResult();
            }
            catch (Exception ex)
            {
                testComplete.SetException(ex);
            }

            return Task.CompletedTask;
        }, options => options.ThrowWriteExceptions = true, LoggerFactory))
        {
            await Assert.ThrowsAsync<HttpRequestException>(() => SendRequestAsync(address));
            await testComplete.Task.DefaultTimeout();
        }
    }

    [ConditionalFact]
    public async Task ResponseSendFile_FirstSendWithCanceledCancellationToken_CancelsAndAborts()
    {
        var testComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (Utilities.CreateHttpServer(out var address, httpContext =>
        {
            try
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                // First write sends headers
                var writeTask = httpContext.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
                Assert.True(writeTask.IsCanceled);
                testComplete.SetResult();
            }
            catch (Exception ex)
            {
                testComplete.SetException(ex);
            }

            return Task.CompletedTask;
        }, LoggerFactory))
        {
            await Assert.ThrowsAsync<HttpRequestException>(() => SendRequestAsync(address));
            await testComplete.Task.DefaultTimeout();
        }
    }

    [ConditionalFact]
    public async Task ResponseSendFileExceptions_SecondSendWithCanceledCancellationToken_CancelsAndAborts()
    {
        var testComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (Utilities.CreateHttpServer(out var address, async httpContext =>
        {
            try
            {
                var cts = new CancellationTokenSource();
                // First write sends headers
                await httpContext.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
                cts.Cancel();
                var writeTask = httpContext.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
                Assert.True(writeTask.IsCanceled);
                testComplete.SetResult();
            }
            catch (Exception ex)
            {
                testComplete.SetException(ex);
            }
        }, options => options.ThrowWriteExceptions = true, LoggerFactory))
        {
            await Assert.ThrowsAsync<HttpRequestException>(() => SendRequestAsync(address));
            await testComplete.Task.DefaultTimeout();
        }
    }

    [ConditionalFact]
    public async Task ResponseSendFile_SecondSendWithCanceledCancellationToken_CancelsAndAborts()
    {
        var testComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (Utilities.CreateHttpServer(out var address, async httpContext =>
        {
            try
            {
                var cts = new CancellationTokenSource();
                // First write sends headers
                await httpContext.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
                cts.Cancel();
                var writeTask = httpContext.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
                Assert.True(writeTask.IsCanceled);
                testComplete.SetResult();
            }
            catch (Exception ex)
            {
                testComplete.SetException(ex);
            }
        }, LoggerFactory))
        {
            await Assert.ThrowsAsync<HttpRequestException>(() => SendRequestAsync(address));
            await testComplete.Task.DefaultTimeout();
        }
    }

    [ConditionalFact]
    public async Task ResponseSendFileExceptions_ClientDisconnectsBeforeFirstSend_SendThrows()
    {
        var requestReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var testComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (Utilities.CreateHttpServer(out var address, async httpContext =>
        {
            httpContext.RequestAborted.Register(() => cancellationReceived.SetResult());
            requestReceived.SetResult();
            await requestCancelled.Task;

            try
            {
                // Note Response.SendFileAsync uses RequestAborted by default. This can cause the response to be disposed
                // before it throws an IOException, but there's a race depending on when the disconnect is noticed.
                // Passing our own token to skip that.
                using var cts = new CancellationTokenSource();
                await Assert.ThrowsAsync<IOException>(async () =>
                {
                    // It can take several tries before Send notices the disconnect.
                    for (int i = 0; i < Utilities.WriteRetryLimit; i++)
                    {
                        await httpContext.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
                    }
                });

                await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                    httpContext.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token));

                testComplete.SetResult();
            }
            catch (Exception ex)
            {
                testComplete.SetException(ex);
            }

        }, options => options.ThrowWriteExceptions = true, LoggerFactory))
        {
            var cts = new CancellationTokenSource();
            var responseTask = SendRequestAsync(address, cts.Token);
            await requestReceived.Task.DefaultTimeout();
            // First write sends headers
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => responseTask);
            requestCancelled.SetResult();

            await testComplete.Task.DefaultTimeout();
            await cancellationReceived.Task.DefaultTimeout();
        }
    }

    [ConditionalFact]
    public async Task ResponseSendFile_ClientDisconnectsBeforeFirstSend_SendCompletesSilently()
    {
        var requestReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var testComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (Utilities.CreateHttpServer(out var address, async httpContext =>
        {
            httpContext.RequestAborted.Register(() => cancellationReceived.SetResult());
            requestReceived.SetResult();
            await requestCancelled.Task;

            try
            {
                // It can take several tries before Send notices the disconnect.
                for (int i = 0; i < Utilities.WriteRetryLimit; i++)
                {
                    await httpContext.Response.SendFileAsync(AbsoluteFilePath, 0, null);
                }

                testComplete.SetResult();
            }
            catch (Exception ex)
            {
                testComplete.SetException(ex);
            }

        }, LoggerFactory))
        {
            var cts = new CancellationTokenSource();
            var responseTask = SendRequestAsync(address, cts.Token);
            await requestReceived.Task.DefaultTimeout();
            // First write sends headers
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => responseTask);
            requestCancelled.SetResult();

            await testComplete.Task.DefaultTimeout();
            await cancellationReceived.Task.DefaultTimeout();
        }
    }

    [ConditionalFact]
    public async Task ResponseSendFileExceptions_ClientDisconnectsBeforeSecondSend_SendThrows()
    {
        var firstSendComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientDisconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var testComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (Utilities.CreateHttpServer(out var address, async httpContext =>
        {
            // Note Response.SendFileAsync uses RequestAborted by default. This can cause the response to be disposed
            // before it throws an IOException, but there's a race depending on when the disconnect is noticed.
            // Passing our own token to skip that.
            using var cts = new CancellationTokenSource();
            httpContext.RequestAborted.Register(() => cancellationReceived.SetResult());
            // First write sends headers
            await httpContext.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
            firstSendComplete.SetResult();
            await clientDisconnected.Task;

            try
            {
                await Assert.ThrowsAsync<IOException>(async () =>
                {
                    // It can take several tries before Write notices the disconnect.
                    for (int i = 0; i < Utilities.WriteRetryLimit; i++)
                    {
                        await httpContext.Response.SendFileAsync(AbsoluteFilePath, 0, null, cts.Token);
                    }
                });

                testComplete.SetResult();
            }
            catch (Exception ex)
            {
                testComplete.SetException(ex);
            }
        }, options => options.ThrowWriteExceptions = true, LoggerFactory))
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(address, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                // Drain data from the connection so that SendFileAsync can complete.
                var bufferTask = response.Content.LoadIntoBufferAsync();

                await firstSendComplete.Task.DefaultTimeout();

                // Abort
                response.Dispose();
            }
            clientDisconnected.SetResult();
            await testComplete.Task.DefaultTimeout();
            await cancellationReceived.Task.DefaultTimeout();
        }
    }

    [ConditionalFact]
    public async Task ResponseSendFile_ClientDisconnectsBeforeSecondSend_SendCompletesSilently()
    {
        var firstSendComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientDisconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var testComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (Utilities.CreateHttpServer(out var address, async httpContext =>
        {
            httpContext.RequestAborted.Register(() => cancellationReceived.SetResult());
            // First write sends headers
            await httpContext.Response.SendFileAsync(AbsoluteFilePath, 0, null);
            firstSendComplete.SetResult();
            await clientDisconnected.Task;

            try
            {
                // It can take several tries before Write notices the disconnect.
                for (int i = 0; i < Utilities.WriteRetryLimit; i++)
                {
                    await httpContext.Response.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);
                }

                testComplete.SetResult();
            }
            catch (Exception ex)
            {
                testComplete.SetException(ex);
            }
        }, LoggerFactory))
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(address, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                // Drain data from the connection so that SendFileAsync can complete.
                var bufferTask = response.Content.LoadIntoBufferAsync();

                await firstSendComplete.Task.DefaultTimeout();

                // Abort
                response.Dispose();
            }
            clientDisconnected.SetResult();
            await testComplete.Task.DefaultTimeout();
            await cancellationReceived.Task.DefaultTimeout();
        }
    }

    private async Task<HttpResponseMessage> SendRequestAsync(string uri, CancellationToken cancellationToken = new CancellationToken())
    {
        using (HttpClient client = new HttpClient() { Timeout = Utilities.DefaultTimeout })
        {
            return await client.GetAsync(uri, cancellationToken);
        }
    }
}
