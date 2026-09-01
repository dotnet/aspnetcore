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
    public async Task LegacyClientRunsNonJSInitializersAndFrameworkAttachmentsInOrder()
    {
        var calls = new List<string>();
        using var provider = CreateServices(
            new TestHostInitializer("lower", -300, calls),
            new TestHostInitializer("user-js", -150, calls, requiresJSInterop: true),
            new TestHostInitializer("later", 0, calls));

        await using var circuitHost = await CreateCircuitHostAsync(
            provider.GetRequiredService<ICircuitFactory>(),
            supportsDeferredHostInitialization: false);

        Assert.Equal(["lower", "later"], calls);
        Assert.Equal("https://localhost/", circuitHost.Services.GetRequiredService<NavigationManager>().BaseUri);
        Assert.True(((RemoteNavigationManager)circuitHost.Services.GetRequiredService<NavigationManager>()).HasAttachedJSRuntime);
        Assert.True(((RemoteNavigationInterception)circuitHost.Services.GetRequiredService<INavigationInterception>()).HasAttachedJSRuntime);
        Assert.True(((RemoteScrollToLocationHash)circuitHost.Services.GetRequiredService<IScrollToLocationHash>()).HasAttachedJSRuntime);
    }

    [Fact]
    public async Task NewClientDefersEntireOrderedSuffixAfterFirstJSInitializer()
    {
        var calls = new List<string>();
        using var provider = CreateServices(
            new TestHostInitializer("lower", -300, calls),
            new TestHostInitializer("user-js", -150, calls, requiresJSInterop: true),
            new TestHostInitializer("later-non-js", 0, calls));

        await using var circuitHost = await CreateCircuitHostAsync(
            provider.GetRequiredService<ICircuitFactory>(),
            supportsDeferredHostInitialization: true);
        var navigationManager = (RemoteNavigationManager)circuitHost.Services.GetRequiredService<NavigationManager>();
        var navigationInterception = (RemoteNavigationInterception)circuitHost.Services.GetRequiredService<INavigationInterception>();
        var scrollToLocationHash = (RemoteScrollToLocationHash)circuitHost.Services.GetRequiredService<IScrollToLocationHash>();

        Assert.Equal(["lower"], calls);
        Assert.False(navigationManager.HasAttachedJSRuntime);
        Assert.False(navigationInterception.HasAttachedJSRuntime);
        Assert.False(scrollToLocationHash.HasAttachedJSRuntime);

        circuitHost.BeginHostInitialization(CancellationToken.None);
        circuitHost.BeginHostInitialization(CancellationToken.None);
        await WaitForAsync(() => calls.Count == 3);

        Assert.Equal(["lower", "user-js", "later-non-js"], calls);
        Assert.True(navigationManager.HasAttachedJSRuntime);
        Assert.True(navigationInterception.HasAttachedJSRuntime);
        Assert.True(scrollToLocationHash.HasAttachedJSRuntime);
    }

    [Fact]
    public async Task NewClientPreservesRegistrationOrderForEqualOrderAcrossDeferredBoundary()
    {
        var calls = new List<string>();
        using var provider = CreateServices(
            new TestHostInitializer("first", -250, calls),
            new TestHostInitializer("second-js", -250, calls, requiresJSInterop: true),
            new TestHostInitializer("third", -250, calls));

        await using var circuitHost = await CreateCircuitHostAsync(
            provider.GetRequiredService<ICircuitFactory>(),
            supportsDeferredHostInitialization: true);

        Assert.Equal(["first"], calls);

        circuitHost.BeginHostInitialization(CancellationToken.None);
        await WaitForAsync(() => calls.Count == 3);

        Assert.Equal(["first", "second-js", "third"], calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EndpointInitializerDoesNotActInInteractiveCircuitScope(bool addServerFirst)
    {
        using var provider = CreateCombinedServices(addServerFirst);

        await using var circuitHost = await CreateCircuitHostAsync(
            provider.GetRequiredService<ICircuitFactory>(),
            supportsDeferredHostInitialization: false);

        var navigationManager = (RemoteNavigationManager)circuitHost.Services.GetRequiredService<NavigationManager>();
        Assert.Equal("https://localhost/", navigationManager.BaseUri);
        Assert.Equal("https://localhost/page", navigationManager.Uri);
        Assert.True(navigationManager.HasAttachedJSRuntime);
    }

    [Fact]
    public async Task NonJSInitializerFailureStopsAndSurfaces()
    {
        var calls = new List<string>();
        var exception = new InvalidOperationException("Initializer failed.");
        using var provider = CreateServices(
            new TestHostInitializer("failure", -300, calls, exception: exception),
            new TestHostInitializer("not-run", -200, calls));

        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await CreateCircuitHostAsync(
                provider.GetRequiredService<ICircuitFactory>(),
                supportsDeferredHostInitialization: true));

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
            new TestHostInitializer("not-run", -200, calls));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await CreateCircuitHostAsync(
                provider.GetRequiredService<ICircuitFactory>(),
                supportsDeferredHostInitialization: true,
                cancellationTokenSource.Token));

        Assert.Equal(["canceled"], calls);
    }

    private static ServiceProvider CreateServices(params IHostInitializer[] initializers)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMetrics();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());
        services.AddServerSideBlazor();
        foreach (var initializer in initializers)
        {
            services.AddSingleton(typeof(IHostInitializer), initializer);
        }

        return services.BuildServiceProvider();
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

    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
        {
            await Task.Delay(10);
        }

        Assert.True(condition());
    }

    private static ValueTask<CircuitHost> CreateCircuitHostAsync(
        ICircuitFactory circuitFactory,
        bool supportsDeferredHostInitialization,
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
            supportsDeferredHostInitialization,
            cancellationToken);

    private sealed class TestHostInitializer(
        string name,
        int order,
        List<string> calls,
        bool requiresJSInterop = false,
        Exception exception = null,
        Action<CancellationToken> callback = null) : IHostInitializer
    {
        public int Order => order;

        public bool RequiresJSInterop => requiresJSInterop;

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            calls.Add(name);
            callback?.Invoke(cancellationToken);

            return exception is null ? Task.CompletedTask : Task.FromException(exception);
        }
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
