// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class KeepAliveTimeoutTests : LoggedTest
{
    private static readonly TimeSpan _keepAliveTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _longDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _shortDelay = TimeSpan.FromSeconds(_longDelay.TotalSeconds / 10);

    private readonly TaskCompletionSource _firstRequestReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    [Fact]
    public async Task ConnectionClosedWhenKeepAliveTimeoutExpires()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = CreateServer(testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.TransportConnection.WaitForReadTask;

                await connection.Send(
                    "GET / HTTP/1.1",
                    "Host:",
                    "",
                    "");
                await ReceiveResponse(connection, testContext);

                // Min amount of time between requests that triggers a keep-alive timeout.
                testContext.FakeTimeProvider.Advance(_keepAliveTimeout + Heartbeat.Interval + TimeSpan.FromTicks(1));
                testContext.ConnectionManager.OnHeartbeat();

                await connection.WaitForConnectionClose();
            }
        }
    }

    [Fact]
    public async Task ConnectionKeptAliveBetweenRequests()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = CreateServer(testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.TransportConnection.WaitForReadTask;

                for (var i = 0; i < 10; i++)
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await ReceiveResponse(connection, testContext);

                    // Max amount of time between requests that doesn't trigger a keep-alive timeout.
                    testContext.FakeTimeProvider.Advance(_keepAliveTimeout + Heartbeat.Interval);
                    testContext.ConnectionManager.OnHeartbeat();
                }
            }
        }
    }

    [Fact]
    public async Task ConnectionNotTimedOutWhileRequestBeingSent()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = CreateServer(testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.TransportConnection.WaitForReadTask;

                await connection.Send(
                        "POST /consume HTTP/1.1",
                        "Host:",
                        "Transfer-Encoding: chunked",
                        "",
                        "");

                await _firstRequestReceived.Task.DefaultTimeout();

                for (var totalDelay = TimeSpan.Zero; totalDelay < _longDelay; totalDelay += _shortDelay)
                {
                    await connection.Send(
                        "1",
                        "a",
                        "");

                    testContext.FakeTimeProvider.Advance(_shortDelay);
                    testContext.ConnectionManager.OnHeartbeat();
                }

                await connection.Send(
                        "0",
                        "",
                        "");
                await ReceiveResponse(connection, testContext);
            }
        }
    }

    [Fact]
    private async Task ConnectionNotTimedOutWhileAppIsRunning()
    {
        var testContext = new TestServiceContext(LoggerFactory);
        var cts = new CancellationTokenSource();

        await using (var server = CreateServer(testContext, longRunningCt: cts.Token))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.TransportConnection.WaitForReadTask;

                await connection.Send(
                    "GET /longrunning HTTP/1.1",
                    "Host:",
                    "",
                    "");

                await _firstRequestReceived.Task.DefaultTimeout();

                for (var totalDelay = TimeSpan.Zero; totalDelay < _longDelay; totalDelay += _shortDelay)
                {
                    testContext.FakeTimeProvider.Advance(_shortDelay);
                    testContext.ConnectionManager.OnHeartbeat();
                }

                cts.Cancel();

                await ReceiveResponse(connection, testContext);

                await connection.Send(
                    "GET / HTTP/1.1",
                    "Host:",
                    "",
                    "");
                await ReceiveResponse(connection, testContext);
            }
        }
    }

    [Fact]
    private async Task ConnectionTimesOutWhenOpenedButNoRequestSent()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = CreateServer(testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.TransportConnection.WaitForReadTask;

                // Min amount of time between requests that triggers a keep-alive timeout.
                testContext.FakeTimeProvider.Advance(_keepAliveTimeout + Heartbeat.Interval + TimeSpan.FromTicks(1));
                testContext.ConnectionManager.OnHeartbeat();

                await connection.WaitForConnectionClose();
            }
        }
    }

    [Fact]
    private async Task KeepAliveTimeoutDoesNotApplyToUpgradedConnections()
    {
        var testContext = new TestServiceContext(LoggerFactory);
        var cts = new CancellationTokenSource();

        await using (var server = CreateServer(testContext, upgradeCt: cts.Token))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.TransportConnection.WaitForReadTask;

                await connection.Send(
                    "GET /upgrade HTTP/1.1",
                    "Host:",
                    "Connection: Upgrade",
                    "",
                    "");
                await connection.Receive(
                    "HTTP/1.1 101 Switching Protocols",
                    "Connection: Upgrade",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");

                for (var totalDelay = TimeSpan.Zero; totalDelay < _longDelay; totalDelay += _shortDelay)
                {
                    testContext.FakeTimeProvider.Advance(_shortDelay);
                    testContext.ConnectionManager.OnHeartbeat();
                }

                cts.Cancel();

                await connection.Receive("hello, world");
            }
        }
    }

    private TestServer CreateServer(TestServiceContext context, CancellationToken longRunningCt = default, CancellationToken upgradeCt = default)
    {
        // Ensure request headers timeout is started as soon as the tests send requests.
        context.Scheduler = PipeScheduler.Inline;
        context.ServerOptions.AddServerHeader = false;
        context.ServerOptions.Limits.KeepAliveTimeout = _keepAliveTimeout;
        context.ServerOptions.Limits.MinRequestBodyDataRate = null;

        return new TestServer(httpContext => App(httpContext, longRunningCt, upgradeCt), context);
    }

    private async Task App(HttpContext httpContext, CancellationToken longRunningCt, CancellationToken upgradeCt)
    {
        var ct = httpContext.RequestAborted;
        var responseStream = httpContext.Response.Body;
        var responseBytes = Encoding.ASCII.GetBytes("hello, world");

        _firstRequestReceived.TrySetResult();

        if (httpContext.Request.Path == "/longrunning")
        {
            await CancellationTokenAsTask(longRunningCt);
        }
        else if (httpContext.Request.Path == "/upgrade")
        {
            using (var stream = await httpContext.Features.Get<IHttpUpgradeFeature>().UpgradeAsync())
            {
                await CancellationTokenAsTask(upgradeCt);

                responseStream = stream;
            }
        }
        else if (httpContext.Request.Path == "/consume")
        {
            var buffer = new byte[1024];
            while (await httpContext.Request.Body.ReadAsync(buffer, 0, buffer.Length) > 0)
            {
                // Read till end
            }
        }

        await responseStream.WriteAsync(responseBytes, 0, responseBytes.Length);
    }

    private async Task ReceiveResponse(InMemoryConnection connection, TestServiceContext testContext)
    {
        await connection.Receive(
            "HTTP/1.1 200 OK",
            $"Date: {testContext.DateHeaderValue}",
            "Transfer-Encoding: chunked",
            "",
            "c",
            "hello, world",
            "0",
            "",
            "");
    }

    private static Task CancellationTokenAsTask(CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        token.Register(() => tcs.SetResult());
        return tcs.Task;
    }
}
