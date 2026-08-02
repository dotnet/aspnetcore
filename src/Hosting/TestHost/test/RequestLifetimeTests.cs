// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.TestHost;

public class RequestLifetimeTests
{
    [Fact]
    public async Task LifetimeFeature_Abort_TriggersRequestAbortedToken()
    {
        var requestAborted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var host = await CreateHost(async httpContext =>
        {
            httpContext.RequestAborted.Register(() => requestAborted.SetResult());
            httpContext.Abort();

            await requestAborted.Task.DefaultTimeout();
        });

        var client = host.GetTestServer().CreateClient();
        var ex = await Assert.ThrowsAsync<OperationCanceledException>(() => client.GetAsync("/", HttpCompletionOption.ResponseHeadersRead));
        Assert.Equal("The application aborted the request.", ex.Message);
        await requestAborted.Task.DefaultTimeout();
    }

    [Fact]
    public async Task LifetimeFeature_AbortBeforeHeadersSent_ClientThrows()
    {
        var abortReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var host = await CreateHost(async httpContext =>
        {
            httpContext.Abort();
            await abortReceived.Task.DefaultTimeout();
        });

        var client = host.GetTestServer().CreateClient();
        var ex = await Assert.ThrowsAsync<OperationCanceledException>(() => client.GetAsync("/", HttpCompletionOption.ResponseHeadersRead));
        Assert.Equal("The application aborted the request.", ex.Message);
        abortReceived.SetResult();
    }

    [Fact]
    public async Task LifetimeFeature_AbortAfterHeadersSent_ClientBodyThrows()
    {
        var responseReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var abortReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var host = await CreateHost(async httpContext =>
        {
            await httpContext.Response.Body.FlushAsync();
            await responseReceived.Task.DefaultTimeout();
            httpContext.Abort();
            await abortReceived.Task.DefaultTimeout();
        });

        var client = host.GetTestServer().CreateClient();
        var response = await client.GetAsync("/", HttpCompletionOption.ResponseHeadersRead);
        responseReceived.SetResult();
        response.EnsureSuccessStatusCode();
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => response.Content.ReadAsByteArrayAsync());
        var rex = ex.GetBaseException();
        Assert.Equal("The application aborted the request.", rex.Message);
        abortReceived.SetResult();
    }

    [Fact]
    public async Task LifetimeFeature_AbortAfterSomeDataSent_ClientBodyThrows()
    {
        var responseReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var abortReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var host = await CreateHost(async httpContext =>
        {
            await httpContext.Response.WriteAsync("Hello World");
            await responseReceived.Task.DefaultTimeout();
            httpContext.Abort();
            await abortReceived.Task.DefaultTimeout();
        });

        var client = host.GetTestServer().CreateClient();
        using var response = await client.GetAsync("/", HttpCompletionOption.ResponseHeadersRead);
        responseReceived.SetResult();
        response.EnsureSuccessStatusCode();
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => response.Content.ReadAsByteArrayAsync());
        var rex = ex.GetBaseException();
        Assert.Equal("The application aborted the request.", rex.Message);
        abortReceived.SetResult();
    }

    // TODO: Abort after CompleteAsync - No-op, the request is already complete.

    private Task<IHost> CreateHost(RequestDelegate appDelegate)
    {
        return new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .Configure(app =>
                    {
                        app.Run(appDelegate);
                    });
            })
            .StartAsync();
    }
}
