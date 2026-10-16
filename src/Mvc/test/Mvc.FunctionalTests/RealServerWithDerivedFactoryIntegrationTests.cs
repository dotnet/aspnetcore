// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RealServerWithDerivedFactoryIntegrationTests
{
    private static int FindFreePort()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }

    private static int GetServerPort<TEntryPoint>(WebApplicationFactory<TEntryPoint> factory) where TEntryPoint : class
    {
        var server = factory.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;
        Assert.NotNull(addresses);
        Assert.NotEmpty(addresses);
        return new Uri(addresses.First()).Port;
    }

    [Fact]
    public async Task UseKestrel_WithSpecificPort_OnDerivedFactory_AppliesPort()
    {
        var expectedPort = FindFreePort();
        await using var factory = new WebApplicationFactory<SimpleWebSite.Startup>()
            .WithWebHostBuilder(_ => { });
        factory.UseKestrel(expectedPort);

        var actualPort = GetServerPort(factory);

        Assert.Equal(expectedPort, actualPort);

        using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{expectedPort}") };
        using var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UseKestrel_WithDynamicPort_OnDerivedFactory_BindsToDynamicPort()
    {
        await using var factory = new WebApplicationFactory<SimpleWebSite.Startup>()
            .WithWebHostBuilder(_ => { });
        factory.UseKestrel(0);

        var actualPort = GetServerPort(factory);

        // Dynamic port must not fall back to Kestrel's default port 5000
        Assert.NotEqual(5000, actualPort);
        Assert.True(actualPort > 0);

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UseKestrel_InheritedFromParentFactory_OnDerivedFactory()
    {
        var expectedPort = FindFreePort();
        await using var parent = new WebApplicationFactory<SimpleWebSite.Startup>();
        parent.UseKestrel(expectedPort);

        await using var derived = parent.WithWebHostBuilder(_ => { });

        var actualPort = GetServerPort(derived);

        Assert.Equal(expectedPort, actualPort);

        using var client = derived.CreateClient();
        using var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UseKestrel_OverridingParentPort_OnDerivedFactory()
    {
        var parentPort = FindFreePort();
        var derivedPort = FindFreePort();
        while (derivedPort == parentPort)
        {
            derivedPort = FindFreePort();
        }

        await using var parent = new WebApplicationFactory<SimpleWebSite.Startup>();
        parent.UseKestrel(parentPort);

        await using var derived = parent.WithWebHostBuilder(_ => { });
        derived.UseKestrel(derivedPort);

        var actualPort = GetServerPort(derived);

        Assert.Equal(derivedPort, actualPort);

        using var client = derived.CreateClient();
        using var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UseKestrel_ChainedDerivedFactories_AppliesPort()
    {
        var expectedPort = FindFreePort();
        await using var factory = new WebApplicationFactory<SimpleWebSite.Startup>()
            .WithWebHostBuilder(_ => { })
            .WithWebHostBuilder(_ => { });
        factory.UseKestrel(expectedPort);

        var actualPort = GetServerPort(factory);

        Assert.Equal(expectedPort, actualPort);

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UseKestrel_WithMinimalApi_OnDerivedFactory_AppliesPort()
    {
        var expectedPort = FindFreePort();
        await using var factory = new WebApplicationFactory<SimpleWebSiteWithWebApplicationBuilder.Program>()
            .WithWebHostBuilder(_ => { });
        factory.UseKestrel(expectedPort);

        var actualPort = GetServerPort(factory);

        Assert.Equal(expectedPort, actualPort);

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
