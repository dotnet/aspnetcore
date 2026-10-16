// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RealServerWithDerivedFactoryIntegrationTests
{
    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    public Task InheritsKestrelWithoutStartingParent(bool useMinimalHosting, int depth) =>
        useMinimalHosting
            ? VerifyInheritedKestrel<SimpleWebSiteWithWebApplicationBuilder.Program>(depth)
            : VerifyInheritedKestrel<SimpleWebSite.Startup>(depth);

    [Fact]
    public Task WebHostInheritsKestrelWithoutStartingParent() =>
        VerifyInheritedKestrel<BasicWebSite.Program>(depth: 2);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task InheritsParameterlessKestrelConfiguration(bool useMinimalHosting) =>
        useMinimalHosting
            ? VerifyInheritedKestrel<SimpleWebSiteWithWebApplicationBuilder.Program>(depth: 2, useDefaultKestrel: true)
            : VerifyInheritedKestrel<SimpleWebSite.Startup>(depth: 2, useDefaultKestrel: true);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task StartedParentDoesNotSupplyDerivedClientAddress(bool useMinimalHosting) =>
        useMinimalHosting
            ? VerifyStartedParent<SimpleWebSiteWithWebApplicationBuilder.Program>()
            : VerifyStartedParent<SimpleWebSite.Startup>();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task InheritsKestrelOptions(bool useMinimalHosting) =>
        useMinimalHosting
            ? VerifyKestrelOptions<SimpleWebSiteWithWebApplicationBuilder.Program>()
            : VerifyKestrelOptions<SimpleWebSite.Startup>();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task CanConfigureKestrelOnDerivedFactory(bool useMinimalHosting) =>
        useMinimalHosting
            ? VerifyDerivedKestrel<SimpleWebSiteWithWebApplicationBuilder.Program>()
            : VerifyDerivedKestrel<SimpleWebSite.Startup>();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task PreservesExplicitClientBaseAddresses(bool useMinimalHosting) =>
        useMinimalHosting
            ? VerifyExplicitClientBaseAddresses<SimpleWebSiteWithWebApplicationBuilder.Program>()
            : VerifyExplicitClientBaseAddresses<SimpleWebSite.Startup>();

    [Fact]
    public async Task UnconfiguredDerivedFactoryStillUsesTestServer()
    {
        await using var parent = new CustomizedFactory<SimpleWebSite.Startup>();
        var derived = CreateDerivedFactory(parent, depth: 2, response: "derived");

        using var client = derived.CreateDefaultClient();

        Assert.IsType<TestServer>(derived.Services.GetRequiredService<IServer>());
        Assert.Equal(new Uri("http://localhost/custom/"), client.BaseAddress);
        Assert.Equal("derived", await client.GetStringAsync("/"));
        Assert.Single(parent.ConfiguredAddresses);
    }

    private static async Task VerifyInheritedKestrel<TEntryPoint>(int depth, bool useDefaultKestrel = false) where TEntryPoint : class
    {
        await using var parent = new CustomizedFactory<TEntryPoint>();
        if (useDefaultKestrel)
        {
            parent.UseKestrel();
        }
        else
        {
            parent.UseKestrel(0);
        }

        var derived = CreateDerivedFactory(parent, depth, response: "derived", overrideUrls: !useDefaultKestrel);
        var sibling = CreateDerivedFactory(parent, depth: 1, response: "sibling");

        using var client = derived.CreateClient();
        using var siblingClient = sibling.CreateClient();
        using var defaultClient = derived.CreateDefaultClient();
        using var handlerClient = derived.CreateDefaultClient(new PassthroughHandler());
        using var networkClient = new HttpClient { BaseAddress = client.BaseAddress };

        var address = GetAddress(derived);
        Assert.Equal(address, client.BaseAddress);
        Assert.NotEqual(address, GetAddress(sibling));
        Assert.Equal(new Uri(address, "custom/"), defaultClient.BaseAddress);
        Assert.Equal(new Uri(address, "custom/"), handlerClient.BaseAddress);
        Assert.Equal("derived", await client.GetStringAsync("/"));
        Assert.Equal("derived", await defaultClient.GetStringAsync("/"));
        Assert.Equal("derived", await handlerClient.GetStringAsync("/"));
        Assert.Equal("derived", await networkClient.GetStringAsync("/"));
        Assert.Equal("sibling", await siblingClient.GetStringAsync("/"));
        Assert.Equal("customized", Assert.Single(client.DefaultRequestHeaders.GetValues("X-Factory")));
        Assert.Equal([address, GetAddress(sibling), address, address], parent.ConfiguredAddresses);
        Assert.Equal(typeof(TEntryPoint) == typeof(BasicWebSite.Program) ? 0 : 2, parent.CreatedHosts);
        Assert.Throws<NotSupportedException>(() => derived.Server);
        Assert.Equal(depth == 2 ? 1 : 0, parent.Factories[0].Factories.Count);
    }

    private static async Task VerifyStartedParent<TEntryPoint>() where TEntryPoint : class
    {
        await using var parent = new CustomizedFactory<TEntryPoint>();
        parent.UseKestrel(0);
        using var parentClient = parent.CreateClient();
        var derived = CreateDerivedFactory(parent, depth: 1, response: "derived");

        using var client = derived.CreateClient();
        using var optionsClient = derived.CreateClient(derived.ClientOptions);

        Assert.NotEqual(parentClient.BaseAddress, client.BaseAddress);
        Assert.Equal(GetAddress(derived), client.BaseAddress);
        Assert.Equal(client.BaseAddress, optionsClient.BaseAddress);
        Assert.Equal("parent", await parentClient.GetStringAsync("/"));
        Assert.Equal("derived", await client.GetStringAsync("/"));
        Assert.Equal("derived", await optionsClient.GetStringAsync("/"));
        Assert.Equal([GetAddress(parent), GetAddress(derived), GetAddress(derived)], parent.ConfiguredAddresses);
    }

    private static async Task VerifyKestrelOptions<TEntryPoint>() where TEntryPoint : class
    {
        await using var parent = new CustomizedFactory<TEntryPoint>();
        var configured = 0;
        parent.UseKestrel(options =>
        {
            configured++;
            options.Listen(IPAddress.Loopback, 0);
            options.Limits.MaxRequestBodySize = 123;
        });
        var derived = CreateDerivedFactory(parent, depth: 2, response: "derived");

        using var client = derived.CreateClient();

        Assert.Equal("derived", await client.GetStringAsync("/"));
        Assert.Equal(1, configured);
        Assert.Equal(123, derived.Services.GetRequiredService<IOptions<KestrelServerOptions>>().Value.Limits.MaxRequestBodySize);
        Assert.Equal(GetAddress(derived), client.BaseAddress);
    }

    private static async Task VerifyDerivedKestrel<TEntryPoint>() where TEntryPoint : class
    {
        await using var parent = new CustomizedFactory<TEntryPoint>();
        var derived = CreateDerivedFactory(parent, depth: 2, response: "derived")
            .WithWebHostBuilder(builder => builder.UseUrls("http://localhost:0"));
        derived.UseKestrel(0);

        using var client = derived.CreateDefaultClient();

        Assert.NotEqual(0, GetAddress(derived).Port);
        Assert.Equal(new Uri(GetAddress(derived), "custom/"), client.BaseAddress);
        Assert.Equal("derived", await client.GetStringAsync("/"));
        Assert.Equal([GetAddress(derived)], parent.ConfiguredAddresses);
    }

    private static async Task VerifyExplicitClientBaseAddresses<TEntryPoint>() where TEntryPoint : class
    {
        await using var parent = new CustomizedFactory<TEntryPoint>();
        parent.UseKestrel(0);
        var baseAddress = new Uri("http://localhost/explicit/");
        parent.ClientOptions.BaseAddress = baseAddress;

        using var parentClient = parent.CreateClient();

        Assert.Equal(baseAddress, parentClient.BaseAddress);

        var derived = CreateDerivedFactory(parent, depth: 2, response: "derived");
        derived.ClientOptions.BaseAddress = baseAddress;

        using var client = derived.CreateClient();
        using var optionsClient = derived.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = baseAddress });

        Assert.Equal(baseAddress, client.BaseAddress);
        Assert.Equal(baseAddress, optionsClient.BaseAddress);
    }

    private static WebApplicationFactory<TEntryPoint> CreateDerivedFactory<TEntryPoint>(
        WebApplicationFactory<TEntryPoint> parent, int depth, string response, bool overrideUrls = false) where TEntryPoint : class
    {
        var derived = parent;
        for (var i = 0; i < depth; i++)
        {
            derived = derived.WithWebHostBuilder(builder =>
            {
                builder.Configure(app => app.Run(context => context.Response.WriteAsync(response)));
                if (overrideUrls)
                {
                    builder.UseUrls("http://localhost:0");
                }
            });
        }

        return derived;
    }

    private static Uri GetAddress<TEntryPoint>(WebApplicationFactory<TEntryPoint> factory) where TEntryPoint : class
    {
        var server = factory.Services.GetRequiredService<IServer>();
        Assert.IsNotType<TestServer>(server);
        var addresses = server.Features.Get<IServerAddressesFeature>();
        Assert.NotNull(addresses);

        return new Uri(Assert.Single(addresses.Addresses));
    }

    private sealed class CustomizedFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
    {
        public List<Uri> ConfiguredAddresses { get; } = [];
        public int CreatedHosts { get; private set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.UseSetting("SkipUrlConfiguration", "true")
                .UseUrls("http://127.0.0.1:0")
                .Configure(app => app.Run(context => context.Response.WriteAsync("parent")));

        protected override IHost CreateHost(IHostBuilder builder)
        {
            CreatedHosts++;

            return base.CreateHost(builder);
        }

        protected override void ConfigureClient(HttpClient client)
        {
            base.ConfigureClient(client);
            ConfiguredAddresses.Add(client.BaseAddress!);
            client.DefaultRequestHeaders.Add("X-Factory", "customized");
            client.BaseAddress = new Uri(client.BaseAddress!, "custom/");
        }
    }

    private sealed class PassthroughHandler : DelegatingHandler;
}
