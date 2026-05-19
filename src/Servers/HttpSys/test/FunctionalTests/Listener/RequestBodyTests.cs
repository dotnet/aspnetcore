// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.Listener;

public class RequestBodyTests
{
    [ConditionalFact]
    public async Task RequestBody_SyncReadDisabledByDefault_WorksWhenEnabled()
    {
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            Task<string> responseTask = SendRequestAsync(address, "Hello World");

            Assert.False(server.Options.AllowSynchronousIO);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            byte[] input = new byte[100];
            Assert.Throws<InvalidOperationException>(() => context.Request.Body.Read(input, 0, input.Length));

            context.AllowSynchronousIO = true;

            Assert.True(context.AllowSynchronousIO);
            var read = context.Request.Body.Read(input, 0, input.Length);
            context.Response.ContentLength = read;
            context.Response.Body.Write(input, 0, read);

            string response = await responseTask;
            Assert.Equal("Hello World", response);
        }
    }

    [ConditionalFact]
    public async Task RequestBody_ReadAsyncAlreadyCanceled_ReturnsCanceledTask()
    {
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            Task<string> responseTask = SendRequestAsync(address, "Hello World");

            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);

            byte[] input = new byte[10];
            var cts = new CancellationTokenSource();
            cts.Cancel();

            Task<int> task = context.Request.Body.ReadAsync(input, 0, input.Length, cts.Token);
            Assert.True(task.IsCanceled);

            context.Dispose();

            string response = await responseTask;
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task RequestBody_ReadAsyncPartialBodyWithCancellationToken_Success()
    {
        StaggardContent content = new StaggardContent();
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            Task<string> responseTask = SendRequestAsync(address, content);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            byte[] input = new byte[10];
            var cts = new CancellationTokenSource();
            int read = await context.Request.Body.ReadAsync(input, 0, input.Length, cts.Token);
            Assert.Equal(5, read);
            content.Block.Release();
            read = await context.Request.Body.ReadAsync(input, 0, input.Length, cts.Token);
            Assert.Equal(5, read);
            context.Dispose();

            string response = await responseTask;
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task RequestBody_ReadAsyncPartialBodyWithTimeout_Success()
    {
        StaggardContent content = new StaggardContent();
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            Task<string> responseTask = SendRequestAsync(address, content);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            byte[] input = new byte[10];
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            int read = await context.Request.Body.ReadAsync(input, 0, input.Length, cts.Token);
            Assert.Equal(5, read);
            content.Block.Release();
            read = await context.Request.Body.ReadAsync(input, 0, input.Length, cts.Token);
            Assert.Equal(5, read);
            context.Dispose();

            string response = await responseTask;
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task RequestBody_ReadAsyncPartialBodyAndCancel_Canceled()
    {
        StaggardContent content = new StaggardContent();
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            Task<string> responseTask = SendRequestAsync(address, content);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            byte[] input = new byte[10];
            var cts = new CancellationTokenSource();
            int read = await context.Request.Body.ReadAsync(input, 0, input.Length, cts.Token);
            Assert.Equal(5, read);
            var readTask = context.Request.Body.ReadAsync(input, 0, input.Length, cts.Token);
            Assert.False(readTask.IsCanceled);
            cts.Cancel();
            await Assert.ThrowsAsync<IOException>(async () => await readTask);
            content.Block.Release();
            context.Dispose();

            await Assert.ThrowsAsync<HttpRequestException>(async () => await responseTask);
        }
    }

    [ConditionalFact]
    public async Task RequestBody_ReadAsyncPartialBodyAndExpiredTimeout_Canceled()
    {
        StaggardContent content = new StaggardContent();
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            Task<string> responseTask = SendRequestAsync(address, content);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            byte[] input = new byte[10];
            var cts = new CancellationTokenSource();
            int read = await context.Request.Body.ReadAsync(input, 0, input.Length, cts.Token);
            Assert.Equal(5, read);
            cts.CancelAfter(TimeSpan.FromMilliseconds(100));
            var readTask = context.Request.Body.ReadAsync(input, 0, input.Length, cts.Token);
            Assert.False(readTask.IsCanceled);
            await Assert.ThrowsAsync<IOException>(async () => await readTask);
            content.Block.Release();
            context.Dispose();

            await Assert.ThrowsAsync<HttpRequestException>(async () => await responseTask);
        }
    }

    // Make sure that using our own disconnect token as a read cancellation token doesn't
    // cause recursion problems when it fires and calls Abort.
    [ConditionalFact]
    public async Task RequestBody_ReadAsyncPartialBodyAndDisconnectedClient_Canceled()
    {
        StaggardContent content = new StaggardContent();
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            var client = new HttpClient();
            var responseTask = client.PostAsync(address, content);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            byte[] input = new byte[10];
            int read = await context.Request.Body.ReadAsync(input, 0, input.Length, context.DisconnectToken);
            Assert.False(context.DisconnectToken.IsCancellationRequested);
            // The client should timeout and disconnect, making this read fail.
            var assertTask = Assert.ThrowsAsync<IOException>(async () => await context.Request.Body.ReadAsync(input, 0, input.Length, context.DisconnectToken));
            client.CancelPendingRequests();
            await assertTask;
            content.Block.Release();
            context.Dispose();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await responseTask);
        }
    }

    private Task<string> SendRequestAsync(string uri, string upload)
    {
        return SendRequestAsync(uri, new StringContent(upload));
    }

    private async Task<string> SendRequestAsync(string uri, HttpContent content)
    {
        using (HttpClient client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            HttpResponseMessage response = await client.PostAsync(uri, content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }

    private class StaggardContent : HttpContent
    {
        public StaggardContent()
        {
            Block = new SemaphoreSlim(0, 1);
        }

        public SemaphoreSlim Block { get; private set; }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            await stream.WriteAsync(new byte[5], 0, 5);
            await stream.FlushAsync();
            await Block.WaitAsync();
            await stream.WriteAsync(new byte[5], 0, 5);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 10;
            return true;
        }
    }
}
