// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.BlazorPack;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Components.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.Extensions.DependencyInjection;

public class ComponentServiceCollectionExtensionsTest
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EqualOrderInitializersPreserveServiceRegistrationOrder(bool registerUserFirst)
    {
        var services = new ServiceCollection();
        var userInitializer = new TestHostInitializer();
        if (registerUserFirst)
        {
            services.AddSingleton<IHostInitializer>(userInitializer);
        }

        services.AddServerSideBlazor();

        if (!registerUserFirst)
        {
            services.AddSingleton<IHostInitializer>(userInitializer);
        }

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var initializers = scope.ServiceProvider.GetServices<IHostInitializer>()
            .OrderBy(initializer => initializer.Order)
            .ToArray();
        var navigationInitializerIndex = Array.FindIndex(
            initializers,
            initializer => initializer.GetType() == typeof(NavigationManagerInitializer));
        var userInitializerIndex = Array.IndexOf(initializers, userInitializer);

        Assert.Equal(registerUserFirst, userInitializerIndex < navigationInitializerIndex);
    }

    [Fact]
    public async Task ServerInitializersDoNotResolveInteractiveServicesInStaticScope()
    {
        var services = new ServiceCollection();
        services.AddServerSideBlazor();
        services.AddScoped<NavigationManager>(_ => throw new InvalidOperationException("Unexpected resolution."));
        services.AddScoped<INavigationInterception>(_ => throw new InvalidOperationException("Unexpected resolution."));
        services.AddScoped<IScrollToLocationHash>(_ => throw new InvalidOperationException("Unexpected resolution."));
        services.AddScoped<IJSRuntime>(_ => throw new InvalidOperationException("Unexpected resolution."));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var initializers = scope.ServiceProvider.GetServices<IHostInitializer>();

        foreach (var initializer in initializers)
        {
            await initializer.InitializeAsync();
        }
    }

    [Fact]
    public void AddServerSideSignalR_RegistersBlazorPack()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddServerSideBlazor();

        // Act
        var options = services.BuildServiceProvider().GetRequiredService<IOptions<HubOptions<ComponentHub>>>();

        // Assert
        var protocol = Assert.Single(options.Value.SupportedProtocols);
        Assert.Equal(BlazorPackHubProtocol.ProtocolName, protocol);
    }

    [Fact]
    public void AddServerSideSignalR_RespectsGlobalHubOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddServerSideBlazor();

        services.Configure<HubOptions>(options =>
        {
            options.SupportedProtocols.Add("test");
            options.HandshakeTimeout = TimeSpan.FromMinutes(10);
        });

        // Act
        var options = services.BuildServiceProvider().GetRequiredService<IOptions<HubOptions<ComponentHub>>>();

        // Assert
        var protocol = Assert.Single(options.Value.SupportedProtocols);
        Assert.Equal(BlazorPackHubProtocol.ProtocolName, protocol);
        Assert.Equal(TimeSpan.FromMinutes(10), options.Value.HandshakeTimeout);
    }

    [Fact]
    public void AddServerSideSignalR_ConfiguresGlobalOptionsBeforePerHubOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddServerSideBlazor().AddHubOptions(options =>
        {
            Assert.Equal(TimeSpan.FromMinutes(10), options.HandshakeTimeout);
            options.HandshakeTimeout = TimeSpan.FromMinutes(5);
        });

        services.Configure<HubOptions>(options =>
        {
            options.SupportedProtocols.Add("test");
            options.HandshakeTimeout = TimeSpan.FromMinutes(10);
        });

        // Act
        var options = services.BuildServiceProvider().GetRequiredService<IOptions<HubOptions<ComponentHub>>>();
        var globalOptions = services.BuildServiceProvider().GetRequiredService<IOptions<HubOptions>>();

        // Assert
        var protocol = Assert.Single(options.Value.SupportedProtocols);
        Assert.Equal(BlazorPackHubProtocol.ProtocolName, protocol);
        Assert.Equal(TimeSpan.FromMinutes(5), options.Value.HandshakeTimeout);

        // Configuring Blazor options is kept separate from the global options.
        Assert.Equal(TimeSpan.FromMinutes(10), globalOptions.Value.HandshakeTimeout);
    }

    [Theory]
    [InlineData("Endpoints")]
    [InlineData("Server")]
    [InlineData("EndpointsThenServer")]
    [InlineData("ServerThenEndpoints")]
    public void HostStartupValuesRegistrationUsesTheExpectedNonInteractiveHolder(string registrations)
    {
        var services = new ServiceCollection();
        switch (registrations)
        {
            case "Endpoints":
                services.AddRazorComponents();
                break;
            case "Server":
                services.AddServerSideBlazor();
                break;
            case "EndpointsThenServer":
                services.AddRazorComponents();
                services.AddServerSideBlazor();
                break;
            case "ServerThenEndpoints":
                services.AddServerSideBlazor();
                services.AddRazorComponents();
                break;
        }

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var startupValues = scope.ServiceProvider.GetRequiredService<IHostStartupValues>();
        var expectedAssembly = registrations is "Server"
            ? "Microsoft.AspNetCore.Components.Server"
            : "Microsoft.AspNetCore.Components.Endpoints";

        Assert.Equal(expectedAssembly, startupValues.GetType().Assembly.GetName().Name);
    }

    [Fact]
    public void HostStartupValuesRegistrationSelectsServerHolderForInteractiveScope()
    {
        var services = new ServiceCollection();
        services.AddRazorComponents();
        services.AddServerSideBlazor();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<InteractiveServerContext>().IsInteractive = true;

        var startupValues = scope.ServiceProvider.GetRequiredService<IHostStartupValues>();
        Assert.IsType<InteractiveHostStartupValues>(startupValues);
    }

    [Fact]
    public void AddServerSideBlazorRepeatedlyDoesNotDuplicateHostStartupValueRegistrations()
    {
        var services = new ServiceCollection();
        services.AddServerSideBlazor();
        var countAfterFirstCall = services.Count(descriptor => descriptor.ServiceType == typeof(IHostStartupValues));

        services.AddServerSideBlazor();

        Assert.Equal(
            countAfterFirstCall,
            services.Count(descriptor => descriptor.ServiceType == typeof(IHostStartupValues)));
        Assert.Equal(3, services.Count(descriptor => descriptor.ServiceType == typeof(IHostInitializer)));
    }

    private sealed class TestHostInitializer : IHostInitializer
    {
        public int Order => -200;

        public Task InitializeAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
