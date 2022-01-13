// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting;

// Most functionality is covered by WebHostBuilderTests for compat. Only GenericHost specific functionality is covered here.
public class GenericWebHostBuilderTests
{
    [Fact]
    public void ReadsAspNetCoreEnvironmentVariables()
    {
        var randomEnvKey = Guid.NewGuid().ToString();
        Environment.SetEnvironmentVariable("ASPNETCORE_" + randomEnvKey, "true");
        using var host = new HostBuilder()
            .ConfigureWebHost(_ => { })
            .Build();
        var config = host.Services.GetRequiredService<IConfiguration>();
        Assert.Equal("true", config[randomEnvKey]);
        Environment.SetEnvironmentVariable("ASPNETCORE_" + randomEnvKey, null);
    }

    [Fact]
    public void CanSuppressAspNetCoreEnvironmentVariables()
    {
        var randomEnvKey = Guid.NewGuid().ToString();
        Environment.SetEnvironmentVariable("ASPNETCORE_" + randomEnvKey, "true");
        using var host = new HostBuilder()
            .ConfigureWebHost(_ => { }, webHostBulderOptions => { webHostBulderOptions.SuppressEnvironmentConfiguration = true; })
            .Build();
        var config = host.Services.GetRequiredService<IConfiguration>();
        Assert.Null(config[randomEnvKey]);
        Environment.SetEnvironmentVariable("ASPNETCORE_" + randomEnvKey, null);
    }

    [Fact]
    public void UseUrlsWorksAfterAppConfigurationSourcesAreCleared()
    {
        var server = new TestServer();

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseServer(server)
                    .UseUrls("TEST_URL")
                    .Configure(_ => { });
            })
            .ConfigureAppConfiguration(configBuilder =>
            {
                configBuilder.Sources.Clear();
            })
            .Build();

        host.Start();

        Assert.Equal("TEST_URL", server.Addresses.Single());
    }

    [Fact]
    public void UseUrlsWorksAfterHostConfigurationSourcesAreCleared()
    {
        var server = new TestServer();

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseServer(server)
                    .UseUrls("TEST_URL")
                    .Configure(_ => { });
            })
            .ConfigureHostConfiguration(configBuilder =>
            {
                configBuilder.Sources.Clear();
            })
            .Build();

        host.Start();

        Assert.Equal("TEST_URL", server.Addresses.Single());
    }

    private class TestServer : IServer, IServerAddressesFeature
    {
        public TestServer()
        {
            Features.Set<IServerAddressesFeature>(this);
        }

        public IFeatureCollection Features { get; } = new FeatureCollection();

        public ICollection<string> Addresses { get; } = new List<string>();
        public bool PreferHostingUrls { get; set; }

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public void Dispose() { }
    }
}
