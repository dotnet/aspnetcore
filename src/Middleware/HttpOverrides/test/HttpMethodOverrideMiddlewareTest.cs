// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.HttpOverrides;

public class HttpMethodOverrideMiddlewareTest
{
    [Fact]
    public async Task XHttpMethodOverrideHeaderAvaiableChangesRequestMethod()
    {
        var assertsExecuted = false;
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHttpMethodOverride();
                    app.Run(context =>
                    {
                        assertsExecuted = true;
                        Assert.Equal("DELETE", context.Request.Method);
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var req = new HttpRequestMessage(HttpMethod.Post, "");
        req.Headers.Add("X-Http-Method-Override", "DELETE");
        await server.CreateClient().SendAsync(req);
        Assert.True(assertsExecuted);
    }

    [Fact]
    public async Task XHttpMethodOverrideHeaderUnavaiableDoesntChangeRequestMethod()
    {
        var assertsExecuted = false;
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHttpMethodOverride();
                    app.Run(context =>
                    {
                        Assert.Equal("POST", context.Request.Method);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var req = new HttpRequestMessage(HttpMethod.Post, "");
        await server.CreateClient().SendAsync(req);
        Assert.True(assertsExecuted);
    }

    [Fact]
    public async Task XHttpMethodOverrideFromGetRequestDoesntChangeMethodType()
    {
        var assertsExecuted = false;
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHttpMethodOverride();
                    app.Run(context =>
                    {
                        Assert.Equal("GET", context.Request.Method);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var req = new HttpRequestMessage(HttpMethod.Get, "");
        await server.CreateClient().SendAsync(req);
        Assert.True(assertsExecuted);
    }

    [Fact]
    public async Task FormFieldAvailableChangesRequestMethod()
    {
        var assertsExecuted = false;
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHttpMethodOverride(new HttpMethodOverrideOptions()
                    {
                        FormFieldName = "_METHOD"
                    });
                    app.Run(context =>
                    {
                        Assert.Equal("DELETE", context.Request.Method);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var req = new HttpRequestMessage(HttpMethod.Post, "");
        req.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "_METHOD", "DELETE" }
            });

        await server.CreateClient().SendAsync(req);
        Assert.True(assertsExecuted);
    }

    [Fact]
    public async Task FormFieldUnavailableDoesNotChangeRequestMethod()
    {
        var assertsExecuted = false;
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHttpMethodOverride(new HttpMethodOverrideOptions()
                    {
                        FormFieldName = "_METHOD"
                    });
                    app.Run(context =>
                    {
                        Assert.Equal("POST", context.Request.Method);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var req = new HttpRequestMessage(HttpMethod.Post, "");
        req.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
        });

        await server.CreateClient().SendAsync(req);
        Assert.True(assertsExecuted);
    }

    [Fact]
    public async Task FormFieldEmptyDoesNotChangeRequestMethod()
    {
        var assertsExecuted = false;
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseHttpMethodOverride(new HttpMethodOverrideOptions()
                    {
                        FormFieldName = "_METHOD"
                    });
                    app.Run(context =>
                    {
                        Assert.Equal("POST", context.Request.Method);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var req = new HttpRequestMessage(HttpMethod.Post, "");
        req.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "_METHOD", "" }
            });

        await server.CreateClient().SendAsync(req);
        Assert.True(assertsExecuted);
    }
}
