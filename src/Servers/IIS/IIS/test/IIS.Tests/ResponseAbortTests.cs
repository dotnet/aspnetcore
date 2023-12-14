// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

[SkipIfHostableWebCoreNotAvailable]
[MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8, SkipReason = "https://github.com/aspnet/IISIntegration/issues/866")]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
public class ResponseAbortTests : StrictTestServerTests
{
    [ConditionalFact]
    public async Task ClosesWithoutSendingAnything()
    {
        using (var testServer = await TestServer.Create(
            ctx =>
            {
                ctx.Abort();
                return Task.CompletedTask;
            }, LoggerFactory))
        {
            using (var connection = testServer.CreateConnection())
            {
                await SendContentLength1Post(connection);
                await connection.WaitForConnectionClose();
            }
        }
    }

    [ConditionalFact]
    public async Task ClosesAfterDataSent()
    {
        var bodyReceived = CreateTaskCompletionSource();
        using (var testServer = await TestServer.Create(
            async ctx =>
            {
                await ctx.Response.WriteAsync("Abort");
                await ctx.Response.Body.FlushAsync();
                await bodyReceived.Task.TimeoutAfter(TimeoutExtensions.DefaultTimeoutValue);
                ctx.Abort();
            }, LoggerFactory))
        {
            using (var connection = testServer.CreateConnection())
            {
                await SendContentLength1Post(connection);
                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "");
                await connection.ReceiveHeaders(
                    "Transfer-Encoding: chunked");

                await connection.ReceiveChunk("Abort");
                bodyReceived.SetResult(true);
                await connection.WaitForConnectionClose();
            }
        }
    }

    [ConditionalFact]
    public async Task ReadsThrowAfterAbort()
    {
        Exception exception = null;

        using (var testServer = await TestServer.Create(
            async ctx =>
            {
                ctx.Abort();
                try
                {
                    var a = new byte[10];
                    await ctx.Request.Body.ReadAsync(a);
                }
                catch (Exception e)
                {
                    exception = e;
                }
            }, LoggerFactory))
        {
            using (var connection = testServer.CreateConnection())
            {
                await SendContentLength1Post(connection);
                await connection.WaitForConnectionClose();
            }
        }

        Assert.IsType<ConnectionAbortedException>(exception);
    }

    [ConditionalFact]
    public async Task WritesNoopAfterAbort()
    {
        Exception exception = null;

        using (var testServer = await TestServer.Create(
            async ctx =>
            {
                ctx.Abort();
                try
                {
                    await ctx.Response.Body.WriteAsync(new byte[10]);
                }
                catch (Exception e)
                {
                    exception = e;
                }
            }, LoggerFactory))
        {
            using (var connection = testServer.CreateConnection())
            {
                await SendContentLength1Post(connection);
                await connection.WaitForConnectionClose();
            }
        }

        Assert.Null(exception);
    }

    [ConditionalFact]
    public async Task RequestAbortedIsTrippedAfterAbort()
    {
        bool tokenAborted = false;
        using (var testServer = await TestServer.Create(
            ctx =>
            {
                ctx.Abort();
                tokenAborted = ctx.RequestAborted.IsCancellationRequested;
                return Task.CompletedTask;
            }, LoggerFactory))
        {
            using (var connection = testServer.CreateConnection())
            {
                await SendContentLength1Post(connection);
                await connection.WaitForConnectionClose();
            }
        }

        Assert.True(tokenAborted);
    }

    [ConditionalFact]
    public async Task CancellationTokenIsUsableAfterAbortingRequest()
    {
        using (var testServer = await TestServer.Create(async ctx =>
        {
            var token = ctx.RequestAborted;
            var originalRegistration = token.Register(() => { });

            ctx.Abort();

            Assert.True(token.WaitHandle.WaitOne(10000));
            Assert.True(ctx.RequestAborted.WaitHandle.WaitOne(10000));
            Assert.Equal(token, originalRegistration.Token);

            await Task.CompletedTask;
        }, LoggerFactory))
        {
            using (var connection = testServer.CreateConnection())
            {
                await SendContentLength1Post(connection);
                await connection.WaitForConnectionClose();
            }
        }
    }

    private static async Task SendContentLength1Post(TestConnection connection)
    {
        await connection.Send(
            "POST / HTTP/1.1",
            "Content-Length: 1",
            "Host: localhost",
            "",
            "");
    }
}
