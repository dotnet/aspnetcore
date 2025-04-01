// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Pipelines;
using System.Reflection;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes;
using Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        var server = Assert.IsType<KestrelServerImpl>(hostBuilder.Build().Services.GetService<IServer>());

        Assert.NotNull(server.ServiceContext.DiagnosticSource);
        Assert.IsType<KestrelMetrics>(server.ServiceContext.Metrics);
        Assert.Equal(PipeScheduler.ThreadPool, server.ServiceContext.Scheduler);
        Assert.Equal(TimeProvider.System, server.ServiceContext.TimeProvider);

        var handlers = (IHeartbeatHandler[])typeof(Heartbeat).GetField("_callbacks", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(server.ServiceContext.Heartbeat);
        Assert.Collection(handlers,
            handler =>
            {
                Assert.Equal(typeof(DateHeaderValueManager), handler.GetType());
            },
            handler =>
            {
                Assert.Equal(typeof(ConnectionManager), handler.GetType());
            },
            handler =>
            {
                Assert.Equal(typeof(PinnedBlockMemoryPoolFactory), handler.GetType());
            });
    }

    [Fact]
    public void MemoryPoolFactorySetCorrectlyWithSockets()
    {
        var hostBuilder = new WebHostBuilder()
            .UseSockets()
            .UseKestrel()
            .Configure(app => { });

        var host = hostBuilder.Build();

        var memoryPoolFactory = Assert.IsType<PinnedBlockMemoryPoolFactory>(host.Services.GetRequiredService<IMemoryPoolFactory<byte>>());
        Assert.Null(host.Services.GetService<IMemoryPoolFactory<int>>());

        Assert.Same(memoryPoolFactory, host.Services.GetRequiredService<IOptions<SocketTransportOptions>>().Value.MemoryPoolFactory);

        // Swap order of UseKestrel and UseSockets
        hostBuilder = new WebHostBuilder()
            .UseKestrel()
            .UseSockets()
            .Configure(app => { });

        host = hostBuilder.Build();

        memoryPoolFactory = Assert.IsType<PinnedBlockMemoryPoolFactory>(host.Services.GetRequiredService<IMemoryPoolFactory<byte>>());
        Assert.Null(host.Services.GetService<IMemoryPoolFactory<int>>());

        Assert.Same(memoryPoolFactory, host.Services.GetRequiredService<IOptions<SocketTransportOptions>>().Value.MemoryPoolFactory);
    }

    [Fact]
    public void SocketsHasDefaultMemoryPool()
    {
        var hostBuilder = new WebHostBuilder()
            .UseSockets()
            .Configure(app => { });

        var host = hostBuilder.Build();

        var memoryPoolFactory = host.Services.GetRequiredService<IMemoryPoolFactory<byte>>();
        Assert.IsNotType<PinnedBlockMemoryPoolFactory>(memoryPoolFactory);
        Assert.Null(host.Services.GetService<IMemoryPoolFactory<int>>());

        Assert.Same(memoryPoolFactory, host.Services.GetRequiredService<IOptions<SocketTransportOptions>>().Value.MemoryPoolFactory);
    }

    [ConditionalFact]
    [NamedPipesSupported]
    public void MemoryPoolFactorySetCorrectlyWithNamedPipes()
    {
        var hostBuilder = new WebHostBuilder()
            .UseNamedPipes()
            .UseKestrel()
            .Configure(app => { });

        var host = hostBuilder.Build();

        var memoryPoolFactory = Assert.IsType<PinnedBlockMemoryPoolFactory>(host.Services.GetRequiredService<IMemoryPoolFactory<byte>>());
        Assert.Null(host.Services.GetService<IMemoryPoolFactory<int>>());

        Assert.Same(memoryPoolFactory, host.Services.GetRequiredService<IOptions<NamedPipeTransportOptions>>().Value.MemoryPoolFactory);

        // Swap order of UseKestrel and UseNamedPipes
        hostBuilder = new WebHostBuilder()
            .UseKestrel()
            .UseNamedPipes()
            .Configure(app => { });

        host = hostBuilder.Build();

        memoryPoolFactory = Assert.IsType<PinnedBlockMemoryPoolFactory>(host.Services.GetRequiredService<IMemoryPoolFactory<byte>>());
        Assert.Null(host.Services.GetService<IMemoryPoolFactory<int>>());

        Assert.Same(memoryPoolFactory, host.Services.GetRequiredService<IOptions<NamedPipeTransportOptions>>().Value.MemoryPoolFactory);
    }

    [ConditionalFact]
    [NamedPipesSupported]
    public void NamedPipesHasDefaultMemoryPool()
    {
        var hostBuilder = new WebHostBuilder()
            .UseNamedPipes()
            .Configure(app => { });

        var host = hostBuilder.Build();

        var memoryPoolFactory = host.Services.GetRequiredService<IMemoryPoolFactory<byte>>();
        Assert.IsNotType<PinnedBlockMemoryPoolFactory>(memoryPoolFactory);
        Assert.Null(host.Services.GetService<IMemoryPoolFactory<int>>());

        Assert.Same(memoryPoolFactory, host.Services.GetRequiredService<IOptions<NamedPipeTransportOptions>>().Value.MemoryPoolFactory);
    }
}
