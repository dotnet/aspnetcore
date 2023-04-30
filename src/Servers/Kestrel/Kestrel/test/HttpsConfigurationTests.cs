// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Server.Kestrel.Tests;

public class HttpsConfigurationTests
{
    [Theory]
    [InlineData("http://127.0.0.1:0", true)]
    [InlineData("http://127.0.0.1:0", false)]
    [InlineData("https://127.0.0.1:0", true)]
    [InlineData("https://127.0.0.1:0", false)]
    public async Task BindAddressFromSetting(string address, bool useKestrelHttpsConfiguration)
    {
        var hostBuilder = new WebHostBuilder()
                .UseKestrelCore()
                .ConfigureKestrel(serverOptions =>
                {
                    serverOptions.TestOverrideDefaultCertificate = new X509Certificate2(Path.Combine("shared", "TestCertificates", "aspnetdevcert.pfx"), "testPassword");
                })
                .Configure(app => { });

        // This is what ASPNETCORE_URLS would populate
        hostBuilder.UseSetting(WebHostDefaults.ServerUrlsKey, address);

        if (useKestrelHttpsConfiguration)
        {
            hostBuilder.UseKestrelHttpsConfiguration();
        }

        var host = hostBuilder.Build();

        Assert.Single(host.ServerFeatures.Get<IServerAddressesFeature>().Addresses, address);

        if (address.StartsWith("https", StringComparison.OrdinalIgnoreCase) && !useKestrelHttpsConfiguration)
        {
            Assert.Throws<InvalidOperationException>(host.Run);
        }
        else
        {
            // Binding succeeds
            await host.StartAsync();
            await host.StopAsync();
        }
    }

    [Fact]
    public void NoFallbackToHttpAddress()
    {
        const string httpAddress = "http://127.0.0.1:0";
        const string httpsAddress = "https://localhost:5001";

        var hostBuilder = new WebHostBuilder()
                .UseKestrelCore()
                .Configure(app => { });

        // This is what ASPNETCORE_URLS would populate
        hostBuilder.UseSetting(WebHostDefaults.ServerUrlsKey, $"{httpAddress};{httpsAddress}");

        var host = hostBuilder.Build();

        Assert.Equal(new[] { httpAddress, httpsAddress }, host.ServerFeatures.Get<IServerAddressesFeature>().Addresses);

        Assert.Throws<InvalidOperationException>(host.Run);
    }

    [Theory]
    [InlineData("http://127.0.0.1:0", true)]
    [InlineData("http://127.0.0.1:0", false)]
    [InlineData("https://127.0.0.1:0", true)]
    [InlineData("https://127.0.0.1:0", false)]
    public async Task BindAddressFromEndpoint(string address, bool useKestrelHttpsConfiguration)
    {
        var hostBuilder = new WebHostBuilder()
                .UseKestrelCore()
                .ConfigureKestrel(serverOptions =>
                {
                    var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
                    {
                        new KeyValuePair<string, string>("Endpoints:end1:Url", address),
                        new KeyValuePair<string, string>("Certificates:Default:Path", Path.Combine("shared", "TestCertificates", "aspnetdevcert.pfx")),
                        new KeyValuePair<string, string>("Certificates:Default:Password", "testPassword"),
                    }).Build();
                    serverOptions.Configure(config);
                })
                .Configure(app => { });

        if (useKestrelHttpsConfiguration)
        {
            hostBuilder.UseKestrelHttpsConfiguration();
        }

        var host = hostBuilder.Build();

        if (address.StartsWith("https", StringComparison.OrdinalIgnoreCase) && !useKestrelHttpsConfiguration)
        {
            Assert.Throws<InvalidOperationException>(host.Run);
        }
        else
        {
            // Binding succeeds
            await host.StartAsync();
            await host.StopAsync();
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LoadDefaultCertificate(bool useKestrelHttpsConfiguration)
    {
        var hostBuilder = new WebHostBuilder()
                .UseKestrelCore()
                .ConfigureKestrel(serverOptions =>
                {
                    var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
                    {
                        new KeyValuePair<string, string>("Certificates:Default:Path", Path.Combine("shared", "TestCertificates", "aspnetdevcert.pfx")),
                        new KeyValuePair<string, string>("Certificates:Default:Password", "testPassword"),
                    }).Build();
                    serverOptions.Configure(config);
                })
                .Configure(app => { });

        if (useKestrelHttpsConfiguration)
        {
            hostBuilder.UseKestrelHttpsConfiguration();
        }

        var host = hostBuilder.Build();

        // There's no exception for specifying a default cert when https config is enabled
        await host.StartAsync();
        await host.StopAsync();
    }

    [Theory]
    [InlineData("http://127.0.0.1:0", true)]
    [InlineData("http://127.0.0.1:0", false)]
    [InlineData("https://127.0.0.1:0", true)]
    [InlineData("https://127.0.0.1:0", false)]
    public async Task LoadEndpointCertificate(string address, bool useKestrelHttpsConfiguration)
    {
        var hostBuilder = new WebHostBuilder()
                .UseKestrelCore()
                .ConfigureKestrel(serverOptions =>
                {
                    var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
                    {
                        new KeyValuePair<string, string>("Endpoints:end1:Url", address),
                        new KeyValuePair<string, string>("Certificates:Default:Path", Path.Combine("shared", "TestCertificates", "aspnetdevcert.pfx")),
                        new KeyValuePair<string, string>("Certificates:Default:Password", "testPassword"),
                    }).Build();
                    serverOptions.Configure(config);
                })
                .Configure(app => { });

        if (useKestrelHttpsConfiguration)
        {
            hostBuilder.UseKestrelHttpsConfiguration();
        }

        var host = hostBuilder.Build();

        if (address.StartsWith("https", StringComparison.OrdinalIgnoreCase) && !useKestrelHttpsConfiguration)
        {
            Assert.Throws<InvalidOperationException>(host.Run);
        }
        else
        {
            // Binding succeeds
            await host.StartAsync();
            await host.StopAsync();
        }
    }

    [Fact]
    public async Task UseHttpsJustWorks()
    {
        var hostBuilder = new WebHostBuilder()
            .UseKestrelCore()
            .ConfigureKestrel(serverOptions =>
            {
                serverOptions.TestOverrideDefaultCertificate = new X509Certificate2(Path.Combine("shared", "TestCertificates", "aspnetdevcert.pfx"), "testPassword");

                serverOptions.ListenAnyIP(0, listenOptions =>
                {
                    listenOptions.UseHttps();
                });
            })
            .Configure(app => { });

        var host = hostBuilder.Build();

        // Binding succeeds
        await host.StartAsync();
        await host.StopAsync();

        Assert.True(host.Services.GetRequiredService<IHttpsConfigurationService>().IsInitialized);
    }

    [Fact]
    public async Task UseHttpsMayNotImplyUseKestrelHttpsConfiguration()
    {
        var hostBuilder = new WebHostBuilder()
            .UseKestrelCore()
            .ConfigureKestrel(serverOptions =>
            {
                serverOptions.ListenAnyIP(0, listenOptions =>
                {
                    listenOptions.UseHttps(new HttpsConnectionAdapterOptions()
                    {
                        ServerCertificate = new X509Certificate2(Path.Combine("shared", "TestCertificates", "aspnetdevcert.pfx"), "testPassword"),
                    });
                });
            })
            .Configure(app => { });

        var host = hostBuilder.Build();

        // Binding succeeds
        await host.StartAsync();
        await host.StopAsync();

        // This is more documentary than normative
        Assert.False(host.Services.GetRequiredService<IHttpsConfigurationService>().IsInitialized);
    }
}
