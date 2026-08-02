// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Routing.FunctionalTests;

public class ShortCircuitTests
{
    [Fact]
    public async Task ShortCircuitTest()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.Use((context, next) =>
                        {
                            context.Response.Headers["NotSet"] = "No!";
                            return next(context);
                        });
                        app.UseEndpoints(b =>
                        {
                            b.Map("/shortcircuit", context =>
                            {
                                context.Response.Headers["Set"] = "Yes!";
                                return Task.CompletedTask;
                            })
                                .ShortCircuit();
                        });
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddRouting();
            })
            .Build();

        using var server = host.GetTestServer();

        await host.StartAsync();

        var response = await server.CreateRequest("/shortcircuit").SendAsync("GET");

        Assert.True(response.Headers.Contains("Set"));
        Assert.False(response.Headers.Contains("NotSet"));
    }

    [Fact]
    public async Task MapShortCircuitTest()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.Use((context, next) =>
                        {
                            context.Response.Headers["NotSet"] = "No!";
                            return next(context);
                        });
                        app.UseEndpoints(b =>
                        {
                            b.MapShortCircuit((int)HttpStatusCode.NotFound, "/shortcircuit");
                        });
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddRouting();
            })
            .Build();

        using var server = host.GetTestServer();

        await host.StartAsync();

        var response1 = await server.CreateRequest("/shortcircuit").SendAsync("GET");
        Assert.Equal(HttpStatusCode.NotFound, response1.StatusCode);
        Assert.False(response1.Headers.Contains("NotSet"));

        var response2 = await server.CreateRequest("/shortcircuit/whatever").SendAsync("GET");
        Assert.Equal(HttpStatusCode.NotFound, response2.StatusCode);
        Assert.False(response2.Headers.Contains("NotSet"));
    }
}
