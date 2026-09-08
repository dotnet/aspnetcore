// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Hosting;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Moq;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

public class CircuitFactoryTest
{
    [Fact]
    public async Task RunsHostPhaseBeforeBrowserPhase()
    {
        var calls = new List<string>();
        using var provider = CreateServices(
            new TestHostInitializer("lower", -300, calls),
            new TestHostInitializer("user-browser", -125, calls, browserPhase: true),
            new TestHostInitializer("later-browser", 0, calls, browserPhase: true));

        await using var circuitHost = await CreateCircuitHostAsync(
            provider.GetRequiredService<ICircuitFactory>());
        var navigationManager = (RemoteNavigationManager)circuitHost.Services.GetRequiredService<NavigationManager>();
        var navigationInterception = (RemoteNavigationInterception)circuitHost.Services.GetRequiredService<INavigationInterception>();
        var scrollToLocationHash = (RemoteScrollToLocationHash)circuitHost.Services.GetRequiredService<IScrollToLocationHash>();

        Assert.Equal(["lower"], calls);
        Assert.False(navigationManager.HasAttachedJSRuntime);
        Assert.False(navigationInterception.HasAttachedJSRuntime);
        Assert.False(scrollToLocationHash.HasAttachedJSRuntime);

        await circuitHost.InitializeAsync(null, default, CancellationToken.None);

        Assert.Equal(["lower", "user-browser", "later-browser"], calls);
        Assert.True(navigationManager.HasAttachedJSRuntime);
        Assert.True(navigationInterception.HasAttachedJSRuntime);
        Assert.True(scrollToLocationHash.HasAttachedJSRuntime);
    }

    [Fact]
    public async Task CircuitUsesSharedAndServerInitializersOnly()
    {
        var calls = new List<string>();
        var services = CreateBaseServices();
        services.AddSingleton<IHostInitializer>(new TestHostInitializer("shared", -300, calls));
        services.AddKeyedSingleton<IHostInitializer>(
            HostInitializerKey.Static,
            new TestHostInitializer("static", -250, calls));
        services.AddKeyedSingleton<IHostInitializer>(
            HostInitializerKey.Server,
            new TestHostInitializer("server", -225, calls));
        services.AddRazorComponents();
        services.AddServerSideBlazor();
        using var provider = services.BuildServiceProvider();

        await using var circuitHost = await CreateCircuitHostAsync(
            provider.GetRequiredService<ICircuitFactory>());

        Assert.Equal(["shared", "server"], calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EndpointInitializerDoesNotActInInteractiveCircuitScope(bool addServerFirst)
    {
        using var provider = CreateCombinedServices(addServerFirst);

        await using var circuitHost = await CreateCircuitHostAsync(
            provider.GetRequiredService<ICircuitFactory>());

        var navigationManager = (RemoteNavigationManager)circuitHost.Services.GetRequiredService<NavigationManager>();
        Assert.Equal("https://localhost/", navigationManager.BaseUri);
        Assert.Equal("https://localhost/page", navigationManager.Uri);
        Assert.False(navigationManager.HasAttachedJSRuntime);

        await circuitHost.InitializeAsync(null, default, CancellationToken.None);

        Assert.True(navigationManager.HasAttachedJSRuntime);
    }

    [Fact]
    public async Task NonJSInitializerFailureStopsAndSurfaces()
    {
        var calls = new List<string>();
        var exception = new InvalidOperationException("Initializer failed.");
        using var provider = CreateServices(
            new TestHostInitializer("failure", -300, calls, exception: exception),
            new TestHostInitializer("not-run", -250, calls));

        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await CreateCircuitHostAsync(
                provider.GetRequiredService<ICircuitFactory>()));

        Assert.Same(exception, actualException);
        Assert.Equal(["failure"], calls);
    }

    [Fact]
    public async Task NonJSInitializerCancellationStopsAndSurfaces()
    {
        var calls = new List<string>();
        using var cancellationTokenSource = new CancellationTokenSource();
        using var provider = CreateServices(
            new TestHostInitializer("canceled", -300, calls, callback: token =>
            {
                cancellationTokenSource.Cancel();
                token.ThrowIfCancellationRequested();
            }),
            new TestHostInitializer("not-run", -250, calls));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await CreateCircuitHostAsync(
                provider.GetRequiredService<ICircuitFactory>(),
                cancellationTokenSource.Token));

        Assert.Equal(["canceled"], calls);
    }

    [Fact]
    public async Task SingletonInitializerUsesTheActiveCircuitScope()
    {
        var initializer = new ScopeRecordingInitializer();
        using var provider = CreateServices(initializer);
        var circuitFactory = provider.GetRequiredService<ICircuitFactory>();

        await using var firstCircuit = await CreateCircuitHostAsync(circuitFactory);
        await using var secondCircuit = await CreateCircuitHostAsync(circuitFactory);

        Assert.Equal(2, initializer.ScopedDependencyIds.Count);
        Assert.NotEqual(initializer.ScopedDependencyIds[0], initializer.ScopedDependencyIds[1]);
        Assert.Same(initializer, Assert.Single(provider.GetServices<IHostInitializer>()));
    }

    private static ServiceProvider CreateServices(params IHostInitializer[] initializers)
    {
        var services = CreateBaseServices();
        services.AddScoped<ScopedDependency>();
        services.AddServerSideBlazor();
        foreach (var initializer in initializers)
        {
            services.AddSingleton(typeof(IHostInitializer), initializer);
        }

        return services.BuildServiceProvider();
    }

    private static ServiceCollection CreateBaseServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMetrics();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());
        return services;
    }

    private static ServiceProvider CreateCombinedServices(bool addServerFirst)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMetrics();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());
        if (addServerFirst)
        {
            services.AddServerSideBlazor();
            services.AddRazorComponents();
        }
        else
        {
            services.AddRazorComponents();
            services.AddServerSideBlazor();
        }

        return services.BuildServiceProvider();
    }

    private static ValueTask<CircuitHost> CreateCircuitHostAsync(
        ICircuitFactory circuitFactory,
        CancellationToken cancellationToken = default)
        => circuitFactory.CreateCircuitHostAsync(
            [],
            new CircuitClientProxy(Mock.Of<ISingleClientProxy>(), "connection"),
            "https://localhost/",
            "https://localhost/page",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["document.baseURI"] = "https://localhost/",
                ["location.href"] = "https://localhost/page",
            },
            new ClaimsPrincipal(),
            store: null,
            resourceCollection: null,
            cancellationToken);

    private sealed class TestHostInitializer(
        string name,
        int order,
        List<string> calls,
        bool browserPhase = false,
        Exception exception = null,
        Action<CancellationToken> callback = null) : IHostInitializer
    {
        public int Order => order;

        public Task InitializeHostAsync(IServiceProvider services, CancellationToken cancellationToken = default)
            => browserPhase ? Task.CompletedTask : Invoke(cancellationToken);

        public Task InitializeBrowserAsync(IServiceProvider services, CancellationToken cancellationToken = default)
            => browserPhase ? Invoke(cancellationToken) : Task.CompletedTask;

        private Task Invoke(CancellationToken cancellationToken)
        {
            calls.Add(name);
            callback?.Invoke(cancellationToken);

            return exception is null ? Task.CompletedTask : Task.FromException(exception);
        }
    }

    private sealed class ScopeRecordingInitializer : IHostInitializer
    {
        public int Order => -300;

        public List<Guid> ScopedDependencyIds { get; } = [];

        public Task InitializeHostAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            ScopedDependencyIds.Add(services.GetRequiredService<ScopedDependency>().Id);
            return Task.CompletedTask;
        }
    }

    private sealed class ScopedDependency
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Test";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "/";
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
