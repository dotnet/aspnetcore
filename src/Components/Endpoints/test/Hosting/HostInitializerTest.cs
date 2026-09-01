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
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EqualOrderInitializersPreserveServiceRegistrationOrder(bool registerUserFirst)
    {
        var services = CreateBaseServices();
        var userInitializer = new TestInitializer("user", -200, []);
        if (registerUserFirst)
        {
            services.AddSingleton<IHostInitializer>(userInitializer);
        }

        services.AddRazorComponents();

        if (!registerUserFirst)
        {
            services.AddSingleton<IHostInitializer>(userInitializer);
        }

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var initializers = scope.ServiceProvider.GetServices<IHostInitializer>()
            .OrderBy(initializer => initializer.Order)
            .ToArray();

        Assert.Equal(2, initializers.Length);
        Assert.Same(userInitializer, initializers[registerUserFirst ? 0 : 1]);
        Assert.IsType<NavigationManagerInitializer>(initializers[registerUserFirst ? 1 : 0]);
    }

    [Fact]
    public void RepeatedRegistrationDoesNotDuplicateFrameworkInitializer()
    {
        var services = CreateBaseServices();

        services.AddRazorComponents();
        services.AddRazorComponents();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.Single(scope.ServiceProvider.GetServices<IHostInitializer>());
    }

    [Fact]
    public async Task FrameworkInitializerDoesNotResolveEndpointServicesForDifferentStartupValueHolder()
    {
        var services = CreateBaseServices();
        services.AddScoped<IHostStartupValues, TestHostStartupValues>();
        services.AddRazorComponents();
        services.AddScoped<NavigationManager>(_ => throw new InvalidOperationException("Unexpected resolution."));
        services.AddScoped<EndpointHtmlRenderer>(_ => throw new InvalidOperationException("Unexpected resolution."));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var initializer = Assert.Single(scope.ServiceProvider.GetServices<IHostInitializer>());

        await initializer.InitializeAsync();
    }

    [Fact]
    public async Task InitializersRunInStableOrderAndSkipJSInterop()
    {
        var calls = new List<string>();
        NavigationManager? navigationManager = null;
        using var provider = CreateServices(
            new TestInitializer("skipped", -300, calls, requiresJSInterop: true),
            new TestInitializer("lower", -100, calls),
            new TestInitializer("first-tie", 0, calls),
            new TestInitializer("second-tie", 0, calls),
            new TestInitializer("navigation", 100, calls, callback: _ =>
            {
                Assert.Equal("https://localhost/subdir/", navigationManager!.BaseUri);
                Assert.Equal("https://localhost/subdir/page?query=value", navigationManager.Uri);
            }));
        using var scope = provider.CreateScope();
        var context = CreateHttpContext(scope.ServiceProvider);
        navigationManager = scope.ServiceProvider.GetRequiredService<NavigationManager>();

        await scope.ServiceProvider.GetRequiredService<EndpointHtmlRenderer>()
            .InitializeStandardComponentServicesAsync(context);

        Assert.Equal(["lower", "first-tie", "second-tie", "navigation"], calls);
    }

    [Fact]
    public async Task InitializerFailureStopsExecutionAndSurfaces()
    {
        var calls = new List<string>();
        using var provider = CreateServices(
            new TestInitializer("first", -400, calls),
            new TestInitializer("failure", -300, calls, exception: new InvalidOperationException("Initializer failed.")),
            new TestInitializer("not-run", -200, calls));
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
            new TestInitializer("canceled", -300, calls, observeCancellation: true),
            new TestInitializer("not-run", -200, calls));
        using var scope = provider.CreateScope();
        var context = CreateHttpContext(scope.ServiceProvider);
        context.RequestAborted = new CancellationToken(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scope.ServiceProvider.GetRequiredService<EndpointHtmlRenderer>()
                .InitializeStandardComponentServicesAsync(context));

        Assert.Equal(["canceled"], calls);
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
        bool requiresJSInterop = false,
        bool observeCancellation = false,
        Exception? exception = null,
        Action<CancellationToken>? callback = null) : IHostInitializer
    {
        public int Order => order;

        public bool RequiresJSInterop => requiresJSInterop;

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            calls.Add(name);
            callback?.Invoke(cancellationToken);
            if (observeCancellation)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (exception is not null)
            {
                return Task.FromException(exception);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class TestHostStartupValues : IHostStartupValues
    {
        public string? GetValue(string key)
            => null;

        public string GetRequired(string key)
            => throw new InvalidOperationException("Unexpected access.");
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
