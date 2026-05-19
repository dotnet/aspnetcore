// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.TestHost;

public class ResponseResetTests
{
    [Fact]
    // Reset is only present for HTTP/2
    public async Task ResetFeature_Http11_Missing()
    {
        using var host = await CreateHost(httpContext =>
        {
            var feature = httpContext.Features.Get<IHttpResetFeature>();
            Assert.Null(feature);
            return Task.CompletedTask;
        });

        var client = host.GetTestServer().CreateClient();
        client.DefaultRequestVersion = HttpVersion.Version11;
        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ResetFeature_Http2_Present()
    {
        using var host = await CreateHost(httpContext =>
        {
            var feature = httpContext.Features.Get<IHttpResetFeature>();
            Assert.NotNull(feature);
            return Task.CompletedTask;
        });

        var client = host.GetTestServer().CreateClient();
        client.DefaultRequestVersion = HttpVersion.Version20;
        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ResetFeature_Http3_Present()
    {
        using var host = await CreateHost(httpContext =>
        {
            var feature = httpContext.Features.Get<IHttpResetFeature>();
            Assert.NotNull(feature);
            return Task.CompletedTask;
        });

        var client = host.GetTestServer().CreateClient();
        client.DefaultRequestVersion = HttpVersion.Version30;
        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ResetFeature_Reset_TriggersRequestAbortedToken()
    {
        var requestAborted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var host = await CreateHost(async httpContext =>
        {
            httpContext.RequestAborted.Register(() => requestAborted.SetResult());

            var feature = httpContext.Features.Get<IHttpResetFeature>();
            feature.Reset(12345);
            await requestAborted.Task.DefaultTimeout();
        });

        var client = host.GetTestServer().CreateClient();
        client.DefaultRequestVersion = HttpVersion.Version20;
        var rex = await Assert.ThrowsAsync<HttpResetTestException>(() => client.GetAsync("/"));
        Assert.Equal("The application reset the request with error code 12345.", rex.Message);
        Assert.Equal(12345, rex.ErrorCode);
        await requestAborted.Task.DefaultTimeout();
    }

    [Fact]
    public async Task ResetFeature_ResetBeforeHeadersSent_ClientThrows()
    {
        var resetReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var host = await CreateHost(async httpContext =>
        {
            var feature = httpContext.Features.Get<IHttpResetFeature>();
            feature.Reset(12345);
            await resetReceived.Task.DefaultTimeout();
        });

        var client = host.GetTestServer().CreateClient();
        client.DefaultRequestVersion = HttpVersion.Version20;
        var rex = await Assert.ThrowsAsync<HttpResetTestException>(() => client.GetAsync("/", HttpCompletionOption.ResponseHeadersRead));
        Assert.Equal("The application reset the request with error code 12345.", rex.Message);
        Assert.Equal(12345, rex.ErrorCode);
        resetReceived.SetResult();
    }

    [Fact]
    public async Task ResetFeature_ResetAfterHeadersSent_ClientBodyThrows()
    {
        var responseReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resetReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var host = await CreateHost(async httpContext =>
        {
            await httpContext.Response.Body.FlushAsync();
            await responseReceived.Task.DefaultTimeout();
            var feature = httpContext.Features.Get<IHttpResetFeature>();
            feature.Reset(12345);
            await resetReceived.Task.DefaultTimeout();
        });

        var client = host.GetTestServer().CreateClient();
        client.DefaultRequestVersion = HttpVersion.Version20;
        var response = await client.GetAsync("/", HttpCompletionOption.ResponseHeadersRead);
        responseReceived.SetResult();
        response.EnsureSuccessStatusCode();
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => response.Content.ReadAsByteArrayAsync());
        var rex = Assert.IsAssignableFrom<HttpResetTestException>(ex.GetBaseException());
        Assert.Equal("The application reset the request with error code 12345.", rex.Message);
        Assert.Equal(12345, rex.ErrorCode);
        resetReceived.SetResult();
    }

    [Fact]
    public async Task ResetFeature_ResetAfterSomeDataSent_ClientBodyThrows()
    {
        var responseReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resetReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var host = await CreateHost(async httpContext =>
        {
            await httpContext.Response.WriteAsync("Hello World");
            await responseReceived.Task.DefaultTimeout();
            var feature = httpContext.Features.Get<IHttpResetFeature>();
            feature.Reset(12345);
            await resetReceived.Task.DefaultTimeout();
        });

        var client = host.GetTestServer().CreateClient();
        client.DefaultRequestVersion = HttpVersion.Version20;
        var response = await client.GetAsync("/", HttpCompletionOption.ResponseHeadersRead);
        responseReceived.SetResult();
        response.EnsureSuccessStatusCode();
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => response.Content.ReadAsByteArrayAsync());
        var rex = Assert.IsAssignableFrom<HttpResetTestException>(ex.GetBaseException());
        Assert.Equal("The application reset the request with error code 12345.", rex.Message);
        Assert.Equal(12345, rex.ErrorCode);
        resetReceived.SetResult();
    }

    // TODO: Reset after CompleteAsync - Not sure how to surface this. CompleteAsync hasn't been implemented yet anyways.

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
