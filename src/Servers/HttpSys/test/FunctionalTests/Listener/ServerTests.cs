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
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.Listener;

public class ServerTests
{
    [ConditionalFact]
    public async Task Server_TokenRegisteredAfterClientDisconnects_CallCanceled()
    {
        var interval = TimeSpan.FromSeconds(1);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            using (var client = new HttpClient())
            {
                var responseTask = client.GetAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);

                client.CancelPendingRequests();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => responseTask);

                var ct = context.DisconnectToken;
                Assert.True(ct.CanBeCanceled, "CanBeCanceled");
                ct.Register(() => canceled.SetResult());
                await canceled.Task.TimeoutAfter(interval);
                Assert.True(ct.IsCancellationRequested, "IsCancellationRequested");

                context.Dispose();
            }
        }
    }

    [ConditionalFact]
    public async Task Server_TokenRegisteredAfterResponseSent_Success()
    {
        var interval = TimeSpan.FromSeconds(1);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            using (var client = new HttpClient())
            {
                var responseTask = client.GetAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Dispose();

                var response = await responseTask;
                response.EnsureSuccessStatusCode();
                Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());

                var ct = context.DisconnectToken;
                Assert.False(ct.CanBeCanceled, "CanBeCanceled");
                ct.Register(() => canceled.SetResult());
                Assert.False(ct.IsCancellationRequested, "IsCancellationRequested");

                Assert.False(canceled.Task.IsCompleted, "canceled");
            }
        }
    }

    [ConditionalFact]
    public async Task Server_ConnectionCloseHeader_CancellationTokenFires()
    {
        var interval = TimeSpan.FromSeconds(1);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            var responseTask = SendRequestAsync(address);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            var ct = context.DisconnectToken;
            Assert.True(ct.CanBeCanceled, "CanBeCanceled");
            Assert.False(ct.IsCancellationRequested, "IsCancellationRequested");
            ct.Register(() => canceled.SetResult());

            context.Response.Headers["Connection"] = "close";

            context.Response.ContentLength = 11;
            var writer = new StreamWriter(context.Response.Body);
            await writer.WriteAsync("Hello World");
            await writer.FlushAsync();

            await canceled.Task.TimeoutAfter(interval);
            Assert.True(ct.IsCancellationRequested, "IsCancellationRequested");

            var response = await responseTask;
            Assert.Equal("Hello World", response);
        }
    }

    [ConditionalFact]
    public async Task Server_SetRejectionVerbosityLevel_Success()
    {
        using (var server = Utilities.CreateHttpServer(out string address))
        {
            server.Options.Http503Verbosity = Http503VerbosityLevel.Limited;
            var responseTask = SendRequestAsync(address);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            context.Dispose();

            var response = await responseTask;
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public void Server_RegisterUnavailablePrefix_ThrowsActionableHttpSysException()
    {
        using var server1 = Utilities.CreateHttpServer(out var address1);

        var options = new HttpSysOptions();
        options.UrlPrefixes.Add(address1);
        using var listener = new HttpSysListener(options, new LoggerFactory());

        var exception = Assert.Throws<HttpSysException>(() => listener.Start());

        Assert.Equal((int)ErrorCodes.ERROR_ALREADY_EXISTS, exception.ErrorCode);
        Assert.Contains($"The prefix '{address1}' is already registered.", exception.Message);
    }

    [ConditionalFact]
    public async Task Server_HotAddPrefix_Success()
    {
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            var responseTask = SendRequestAsync(address);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            Assert.Equal(string.Empty, context.Request.PathBase);
            Assert.Equal("/", context.Request.Path);
            context.Dispose();

            var response = await responseTask;
            Assert.Equal(string.Empty, response);

            address += "pathbase/";
            server.Options.UrlPrefixes.Add(address);

            responseTask = SendRequestAsync(address);

            context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            Assert.Equal("/pathbase", context.Request.PathBase);
            Assert.Equal("/", context.Request.Path);
            context.Dispose();

            response = await responseTask;
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task Server_HotRemovePrefix_Success()
    {
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            address += "pathbase/";
            server.Options.UrlPrefixes.Add(address);
            var responseTask = SendRequestAsync(address);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            Assert.Equal("/pathbase", context.Request.PathBase);
            Assert.Equal("/", context.Request.Path);
            context.Dispose();

            var response = await responseTask;
            Assert.Equal(string.Empty, response);

            Assert.True(server.Options.UrlPrefixes.Remove(address));

            responseTask = SendRequestAsync(address);

            context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            Assert.Equal(string.Empty, context.Request.PathBase);
            Assert.Equal("/pathbase/", context.Request.Path);
            context.Dispose();

            response = await responseTask;
            Assert.Equal(string.Empty, response);
        }
    }

    private async Task<string> SendRequestAsync(string uri)
    {
        using (HttpClient client = new HttpClient())
        {
            return await client.GetStringAsync(uri);
        }
    }
}
