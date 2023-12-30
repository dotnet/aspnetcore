// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Tests;

public class WebHostBuilderKestrelExtensionsTests
{
    [Fact]
    public void ApplicationServicesNotNullAfterUseKestrelWithoutOptions()
    {
        // Arrange
        var hostBuilder = new WebHostBuilder()
            .UseKestrel()
            .Configure(app => { });

        hostBuilder.ConfigureServices(services =>
        {
            services.Configure<KestrelServerOptions>(options =>
            {
                // Assert
                Assert.NotNull(options.ApplicationServices);
            });
        });

        // Act
        hostBuilder.Build();
    }

    [Fact]
    public void ApplicationServicesNotNullDuringUseKestrelWithOptions()
    {
        // Arrange
        var hostBuilder = new WebHostBuilder()
            .UseKestrel(options =>
            {
                // Assert
                Assert.NotNull(options.ApplicationServices);
            })
            .Configure(app => { });

        // Act
        hostBuilder.Build();
    }

    [Fact]
    public void DefaultTransportFactoriesConfigured()
    {
        var hostBuilder = new WebHostBuilder()
            .UseKestrel()
            .Configure(app => { });

        var transportFactories = hostBuilder.Build().Services.GetServices<IConnectionListenerFactory>();

        if (OperatingSystem.IsWindows())
        {
            Assert.Collection(transportFactories,
                t => Assert.IsType<SocketTransportFactory>(t),
                t => Assert.IsType<NamedPipeTransportFactory>(t));
        }
        else
        {
            Assert.Collection(transportFactories,
                t => Assert.IsType<SocketTransportFactory>(t));
        }
    }

    [Fact]
    public void SocketsTransportCanBeManuallySelectedIndependentOfOrder()
    {
        var hostBuilder = new WebHostBuilder()
            .UseKestrel()
            .UseSockets()
            .Configure(app => { });

        var factories = hostBuilder.Build().Services.GetServices<IConnectionListenerFactory>();
        AssertContainsType<SocketTransportFactory, IConnectionListenerFactory>(factories);

        var hostBuilderReversed = new WebHostBuilder()
            .UseSockets()
            .UseKestrel()
            .Configure(app => { });

        var factoriesReversed = hostBuilderReversed.Build().Services.GetServices<IConnectionListenerFactory>();
        AssertContainsType<SocketTransportFactory, IConnectionListenerFactory>(factoriesReversed);

        static void AssertContainsType<TExpected, TCollection>(IEnumerable<TCollection> enumerable)
        {
            Assert.Contains(enumerable, f => f is TExpected);
        }
    }

    [Fact]
    public void ServerIsKestrelServerImpl()
    {
        var hostBuilder = new WebHostBuilder()
            .UseSockets()
            .UseKestrel()
            .Configure(app => { });

        Assert.IsType<KestrelServerImpl>(hostBuilder.Build().Services.GetService<IServer>());
    }
}
