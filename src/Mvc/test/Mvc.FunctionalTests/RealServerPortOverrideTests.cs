// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RealServerPortOverrideTests
{
    [Fact]
    public async Task PortOverrideIsAppliedWithKestrelOptionsCallback()
    {
        await using var factory = new ConfiguredFactory<SimpleWebSite.Startup>(
            builder => builder.UseUrls("http://127.0.0.1:1"));
        var configured = false;
        factory.UseKestrel(0);
        factory.UseKestrel(_ => configured = true);

        await AssertDynamicLoopbackEndpoint(factory);
        Assert.True(configured);
    }

    [Fact]
    public async Task PortOverrideTakesPrecedenceOverPreferredHostingUrls()
    {
        await using var factory = new ConfiguredFactory<SimpleWebSite.Startup>(
            builder => builder.UseUrls("http://127.0.0.1:1").PreferHostingUrls(true));
        factory.UseKestrel(0);

        await AssertDynamicLoopbackEndpoint(factory);
    }

    [Fact]
    public async Task PortOverrideReplacesCodeAndConfigurationEndpoints()
    {
        await using var factory = new ConfiguredFactory<SimpleWebSite.Startup>(builder =>
        {
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string> { ["Kestrel:Endpoints:Configured:Url"] = "http://127.0.0.1:0" }));
            builder.ConfigureKestrel((context, options) =>
            {
                options.Configure(context.Configuration.GetSection("Kestrel"));
                options.Listen(IPAddress.Loopback, 0);
            });
        });
        factory.UseKestrel(0);

        await AssertDynamicLoopbackEndpoint(factory);
    }

    [Fact]
    public async Task PortOverrideDoesNotDuplicateApplicationListener()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        await using var factory = new ConfiguredFactory<SimpleWebSite.Startup>(
            builder => builder.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, port)));
        factory.UseKestrel(port);

        var address = await AssertLoopbackEndpoint(factory);
        Assert.Equal(port, address.Port);
    }

    [Fact]
    public async Task PortOverrideReplacesMinimalApplicationEndpoints()
    {
        await using var factory = new ConfiguredFactory<SimpleWebSiteWithWebApplicationBuilder.Program>(
            builder => builder.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0)));
        factory.UseKestrel(0);

        await AssertDynamicLoopbackEndpoint(factory);
    }

    [Fact]
    public async Task PortOverrideIsAppliedWhenStartupFilterShortCircuits()
    {
        await using var factory = new ConfiguredFactory<SimpleWebSite.Startup>(builder =>
        {
            builder.UseUrls("http://127.0.0.1:1");
            builder.ConfigureServices(services => services.AddSingleton<IStartupFilter>(new ShortCircuitStartupFilter()));
        });
        factory.UseKestrel(0);

        await AssertDynamicLoopbackEndpoint(factory);
    }

    private static async Task AssertDynamicLoopbackEndpoint<TEntryPoint>(WebApplicationFactory<TEntryPoint> factory)
        where TEntryPoint : class
    {
        var address = await AssertLoopbackEndpoint(factory);
        Assert.True(address.Port > 1);
    }

    private static async Task<Uri> AssertLoopbackEndpoint<TEntryPoint>(WebApplicationFactory<TEntryPoint> factory)
        where TEntryPoint : class
    {
        using var client = factory.CreateClient();
        var server = factory.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>()!.Addresses;
        var address = new Uri(Assert.Single(addresses));
        Assert.Equal(IPAddress.Loopback.ToString(), address.Host);
        Assert.Equal(address, client.BaseAddress);

        using var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return address;
    }

    private sealed class ConfiguredFactory<TEntryPoint>(Action<IWebHostBuilder> configure)
        : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => configure(builder);
    }

    private sealed class ShortCircuitStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app => app.Run(_ => Task.CompletedTask);
    }
}
