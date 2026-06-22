// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.Gateway;

internal static class GatewayTestHelpers
{
    public static Task<GatewayUnderTest> StartGatewayAsync(string environment) =>
        StartGatewayAsync(environment, new Dictionary<string, string?>());

    public static async Task<GatewayUnderTest> StartGatewayAsync(
        string environment,
        Dictionary<string, string?> configuration)
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            EnvironmentName = environment,
        });

        if (configuration.Count > 0)
        {
            builder.Configuration.AddInMemoryCollection(configuration);
        }

        builder.WebHost.UseTestServer();

        var app = BlazorGateway.BuildWebHost(builder);
        await app.StartAsync();

        return new GatewayUnderTest(app);
    }

    /// <summary>
    /// Spins up a real Kestrel-hosted upstream <see cref="WebApplication"/> on a random
    /// loopback port. The gateway's reverse proxy can be configured to forward to the
    /// resulting <see cref="UpstreamApp.BaseUrl"/>.
    /// </summary>
    public static async Task<UpstreamApp> StartUpstreamAsync(Action<WebApplication> configure)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();

        var app = builder.Build();
        configure(app);
        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.First();

        return new UpstreamApp(app, address);
    }
}

internal sealed class GatewayUnderTest(WebApplication app) : IAsyncDisposable
{
    public WebApplication App { get; } = app;
    public HttpClient Client { get; } = app.GetTestClient();

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await App.DisposeAsync();
    }
}

internal sealed class UpstreamApp(WebApplication app, string baseUrl) : IAsyncDisposable
{
    public WebApplication App { get; } = app;
    public string BaseUrl { get; } = baseUrl;

    public async ValueTask DisposeAsync() => await App.DisposeAsync();
}
