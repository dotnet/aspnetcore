// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrelCore()
                    .ConfigureKestrel(serverOptions =>
                    {
                        serverOptions.TestOverrideDefaultCertificate = new X509Certificate2(Path.Combine("shared", "TestCertificates", "aspnetdevcert.pfx"), "testPassword");
                    })
                    .Configure(app => { })
                    // This is what ASPNETCORE_URLS would populate
                    .UseSetting(WebHostDefaults.ServerUrlsKey, address);

                if (useKestrelHttpsConfiguration)
                {
                    webHostBuilder.UseKestrelHttpsConfiguration();
                }
            });

        using var host = hostBuilder.Build();

        if (address.StartsWith("https", StringComparison.OrdinalIgnoreCase) && !useKestrelHttpsConfiguration)
        {
            Assert.Throws<InvalidOperationException>(host.Start);
            Assert.Empty(host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses);
        }
        else
        {
            await host.StartAsync();

            var addr = Assert.Single(host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses);
            // addr will contain the realized port, so we'll remove the port for comparison
            Assert.Equal(address[..^2].ToString(), addr.Substring(0, addr.LastIndexOf(':')));
        }
    }

    [Fact]
    public void NoFallbackToHttpAddress()
    {
        const string httpAddress = "http://127.0.0.1:0";
        const string httpsAddress = "https://localhost:5001";

        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrelCore()
                    .Configure(app => { })
                    // This is what ASPNETCORE_URLS would populate
                    .UseSetting(WebHostDefaults.ServerUrlsKey, $"{httpAddress};{httpsAddress}");
            });

        var host = hostBuilder.Build();

        var ex = Assert.Throws<InvalidOperationException>(host.Start);
        Assert.Contains("Call UseKestrelHttpsConfiguration()", ex.Message);

        var addr = Assert.Single(host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses);
        // addr will contain the realized port, so we'll remove the port for comparison
        Assert.Equal(httpAddress[..^2].ToString(), addr.Substring(0, addr.LastIndexOf(':')));
    }

    [Theory]
    [InlineData("http://127.0.0.1:0", true)]
    [InlineData("http://127.0.0.1:0", false)]
    [InlineData("https://127.0.0.1:0", true)]
    [InlineData("https://127.0.0.1:0", false)]
    public async Task BindAddressFromEndpoint(string address, bool useKestrelHttpsConfiguration)
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
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
                    webHostBuilder.UseKestrelHttpsConfiguration();
                }
            });

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
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
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
                    webHostBuilder.UseKestrelHttpsConfiguration();
                }
            });

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
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
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
                    webHostBuilder.UseKestrelHttpsConfiguration();
                }
            });

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
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
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
            });

        var host = hostBuilder.Build();

        // Binding succeeds
        await host.StartAsync();
        await host.StopAsync();

        Assert.True(host.Services.GetRequiredService<IHttpsConfigurationService>().IsInitialized);
    }

    [Fact]
    public async Task UseHttpsMayNotImplyUseKestrelHttpsConfiguration()
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
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
            });

        var host = hostBuilder.Build();

        // Binding succeeds
        await host.StartAsync();
        await host.StopAsync();

        // This is more documentary than normative
        Assert.False(host.Services.GetRequiredService<IHttpsConfigurationService>().IsInitialized);
    }
}
