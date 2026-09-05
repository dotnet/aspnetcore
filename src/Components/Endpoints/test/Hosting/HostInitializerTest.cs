// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Components.Endpoints.Tests.Hosting;

public class HostInitializerTest
{
    [Fact]
    public async Task CollectionSortsInitializersByUniqueOrder()
    {
        var calls = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<IHostInitializer>(new TestInitializer("later", 20, calls));
        services.AddSingleton<IHostInitializer>(new TestInitializer("first", -20, calls));
        services.AddSingleton<HostInitializerCollection>();

        using var provider = services.BuildServiceProvider();
        var invoker = provider.GetRequiredService<HostInitializerCollection>()
            .GetInitializerInvoker(provider);

        await invoker.InitializeHostAsync();

        Assert.Equal(["first", "later"], calls);
    }

    [Fact]
    public void CollectionRejectsDuplicateSharedOrders()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostInitializer, FirstDuplicateInitializer>();
        services.AddSingleton<IHostInitializer, SecondDuplicateInitializer>();
        services.AddSingleton<HostInitializerCollection>();

        using var provider = services.BuildServiceProvider();
        var exception = Assert.Throws<InvalidOperationException>(
            provider.GetRequiredService<HostInitializerCollection>);

        Assert.Contains(typeof(FirstDuplicateInitializer).FullName!, exception.Message);
        Assert.Contains(typeof(SecondDuplicateInitializer).FullName!, exception.Message);
        Assert.Contains("Order '0'", exception.Message);
    }

    [Fact]
    public void CollectionRejectsDuplicateSharedAndKeyedOrders()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostInitializer, FirstDuplicateInitializer>();
        services.AddKeyedSingleton<IHostInitializer, SecondDuplicateInitializer>(HostInitializerKey.Static);
        services.AddSingleton<HostInitializerCollection>();

        using var provider = services.BuildServiceProvider();
        var exception = Assert.Throws<InvalidOperationException>(
            provider.GetRequiredService<HostInitializerCollection>);

        Assert.Contains(typeof(FirstDuplicateInitializer).FullName!, exception.Message);
        Assert.Contains(typeof(SecondDuplicateInitializer).FullName!, exception.Message);
        Assert.Contains("Order '0'", exception.Message);
    }

    [Fact]
    public async Task EqualOrdersInDifferentKeyedSetsAreIsolated()
    {
        var calls = new List<string>();
        var staticInitializer = new TestInitializer("static", 0, calls);
        var serverInitializer = new TestInitializer("server", 0, calls);
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IHostInitializer>(HostInitializerKey.Static, staticInitializer);
        services.AddKeyedSingleton<IHostInitializer>(HostInitializerKey.Server, serverInitializer);
        services.AddSingleton<HostInitializerCollection>();

        using var provider = services.BuildServiceProvider();
        var collection = provider.GetRequiredService<HostInitializerCollection>();

        await collection.GetInitializerInvoker(provider, HostInitializerKey.Static).InitializeHostAsync();
        await collection.GetInitializerInvoker(provider, HostInitializerKey.Server).InitializeHostAsync();

        Assert.Equal(["static", "server"], calls);
    }

    [Fact]
    public async Task InvokerCachesHostAndBrowserTasksAndBrowserAwaitsHost()
    {
        var calls = new List<string>();
        var continueHost = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var initializer = new TestInitializer(
            "initializer",
            0,
            calls,
            hostCallback: _ => continueHost.Task,
            browserCallback: _ =>
            {
                calls.Add("browser");
                return Task.CompletedTask;
            });
        var invoker = CreateInvoker(initializer);

        var hostTask = invoker.InitializeHostAsync();
        var browserTask = invoker.InitializeBrowserAsync();

        Assert.Same(hostTask, invoker.InitializeHostAsync());
        Assert.Same(browserTask, invoker.InitializeBrowserAsync());
        Assert.Equal(["initializer"], calls);
        Assert.False(browserTask.IsCompleted);

        continueHost.SetResult();
        await browserTask;

        Assert.Equal(["initializer", "browser"], calls);
        Assert.Same(hostTask, invoker.InitializeHostAsync());
    }

    [Fact]
    public async Task InvokerCachesFailureAndCancellation()
    {
        var failure = new InvalidOperationException("Initializer failed.");
        var failedInvoker = CreateInvoker(new TestInitializer(
            "failure",
            0,
            [],
            hostCallback: _ => Task.FromException(failure)));
        var canceledInvoker = CreateInvoker(new TestInitializer(
            "canceled",
            0,
            [],
            hostCallback: token => Task.FromCanceled(token)));
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var failedTask = failedInvoker.InitializeHostAsync();
        var canceledTask = canceledInvoker.InitializeHostAsync(cancellationTokenSource.Token);

        Assert.Same(failedTask, failedInvoker.InitializeHostAsync());
        Assert.Same(canceledTask, canceledInvoker.InitializeHostAsync());
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => failedTask));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledTask);
    }

    [Fact]
    public async Task InvokerCachesBrowserFailureAndCancellation()
    {
        var failure = new InvalidOperationException("Browser initialization failed.");
        var failedInvoker = CreateInvoker(new TestInitializer(
            "failure",
            0,
            [],
            browserCallback: _ => Task.FromException(failure)));
        var canceledInvoker = CreateInvoker(new TestInitializer(
            "canceled",
            0,
            [],
            browserCallback: token => Task.FromCanceled(token)));
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var failedTask = failedInvoker.InitializeBrowserAsync();
        var canceledTask = canceledInvoker.InitializeBrowserAsync(cancellationTokenSource.Token);

        Assert.Same(failedTask, failedInvoker.InitializeBrowserAsync());
        Assert.Same(canceledTask, canceledInvoker.InitializeBrowserAsync());
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => failedTask));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledTask);
    }

    [Fact]
    public async Task SeparateInvokersUseActiveScopesWithSharedSingletonInitializer()
    {
        var initializer = new ScopeRecordingInitializer();
        var services = new ServiceCollection();
        services.AddScoped<ScopedDependency>();
        services.AddSingleton<IHostInitializer>(initializer);
        services.AddSingleton<HostInitializerCollection>();

        using var provider = services.BuildServiceProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var collection = provider.GetRequiredService<HostInitializerCollection>();

        await collection.GetInitializerInvoker(firstScope.ServiceProvider).InitializeHostAsync();
        await collection.GetInitializerInvoker(secondScope.ServiceProvider).InitializeHostAsync();

        Assert.Equal(2, initializer.ScopedDependencyIds.Count);
        Assert.NotEqual(initializer.ScopedDependencyIds[0], initializer.ScopedDependencyIds[1]);
    }

    [Fact]
    public async Task EndpointUsesSharedAndStaticInitializersOnly()
    {
        var calls = new List<string>();
        var services = CreateBaseServices();
        services.AddSingleton<IHostInitializer>(new TestInitializer("shared", -100, calls));
        services.AddKeyedSingleton<IHostInitializer>(
            HostInitializerKey.Static,
            new TestInitializer("static", 100, calls));
        services.AddKeyedSingleton<IHostInitializer>(
            HostInitializerKey.Server,
            new TestInitializer("server", 200, calls));
        services.AddRazorComponents();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = CreateHttpContext(scope.ServiceProvider);

        await scope.ServiceProvider.GetRequiredService<EndpointHtmlRenderer>()
            .InitializeStandardComponentServicesAsync(context);

        Assert.Equal(["shared", "static"], calls);
    }

    [Fact]
    public void RepeatedRegistrationDoesNotDuplicateFrameworkInitializer()
    {
        var services = CreateBaseServices();

        services.AddRazorComponents();
        services.AddRazorComponents();

        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostInitializer) && !descriptor.IsKeyedService);
        Assert.Single(services.Where(
            descriptor => descriptor.ServiceType == typeof(IHostInitializer) &&
                Equals(descriptor.ServiceKey, HostInitializerKey.Static)));
        Assert.Equal(
            ServiceLifetime.Singleton,
            Assert.Single(services.Where(
                descriptor => descriptor.ServiceType == typeof(HostInitializerCollection))).Lifetime);
    }

    [Fact]
    public async Task InitializerFailureStopsExecutionAndSurfaces()
    {
        var calls = new List<string>();
        using var provider = CreateServices(
            new TestInitializer("first", -400, calls),
            new TestInitializer(
                "failure",
                -300,
                calls,
                hostCallback: _ => Task.FromException(new InvalidOperationException("Initializer failed."))),
            new TestInitializer("not-run", -100, calls));
        using var scope = provider.CreateScope();
        var context = CreateHttpContext(scope.ServiceProvider);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<EndpointHtmlRenderer>()
                .InitializeStandardComponentServicesAsync(context));

        Assert.Equal("Initializer failed.", exception.Message);
        Assert.Equal(["first", "failure"], calls);
    }

    [Fact]
    public async Task InitializerCancellationStopsExecutionAndSurfaces()
    {
        var calls = new List<string>();
        using var provider = CreateServices(
            new TestInitializer(
                "canceled",
                -300,
                calls,
                hostCallback: token => Task.FromCanceled(token)),
            new TestInitializer("not-run", -100, calls));
        using var scope = provider.CreateScope();
        var context = CreateHttpContext(scope.ServiceProvider);
        context.RequestAborted = new CancellationToken(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scope.ServiceProvider.GetRequiredService<EndpointHtmlRenderer>()
                .InitializeStandardComponentServicesAsync(context));

        Assert.Equal(["canceled"], calls);
    }

    private static HostInitializerInvoker CreateInvoker(IHostInitializer initializer)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostInitializer>(initializer);
        services.AddSingleton<HostInitializerCollection>();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<HostInitializerCollection>().GetInitializerInvoker(provider);
    }

    private static ServiceProvider CreateServices(params IHostInitializer[] initializers)
    {
        var services = CreateBaseServices();
        services.AddRazorComponents();
        foreach (var initializer in initializers)
        {
            services.AddSingleton(typeof(IHostInitializer), initializer);
        }

        return services.BuildServiceProvider();
    }

    private static ServiceCollection CreateBaseServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());
        return services;
    }

    private static DefaultHttpContext CreateHttpContext(IServiceProvider services)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = services,
        };
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");
        context.Request.PathBase = "/subdir";
        context.Request.Path = "/page";
        context.Request.QueryString = new QueryString("?query=value");

        return context;
    }

    private sealed class TestInitializer(
        string name,
        int order,
        List<string> calls,
        Func<CancellationToken, Task>? hostCallback = null,
        Func<CancellationToken, Task>? browserCallback = null) : IHostInitializer
    {
        public int Order => order;

        public Task InitializeHostAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            calls.Add(name);
            return hostCallback?.Invoke(cancellationToken) ?? Task.CompletedTask;
        }

        public Task InitializeBrowserAsync(IServiceProvider services, CancellationToken cancellationToken = default)
            => browserCallback?.Invoke(cancellationToken) ?? Task.CompletedTask;
    }

    private sealed class FirstDuplicateInitializer : IHostInitializer;

    private sealed class SecondDuplicateInitializer : IHostInitializer;

    private sealed class ScopeRecordingInitializer : IHostInitializer
    {
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
