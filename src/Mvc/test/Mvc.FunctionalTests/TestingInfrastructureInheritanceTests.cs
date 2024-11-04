// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class TestingInfrastructureInheritanceTests
{
    [Fact]
    public void TestingInfrastructure_WebHost_WithWebHostBuilderRespectsCustomizations()
    {
        // Act
        using var factory = new CustomizedFactory<BasicWebSite.StartupWithoutEndpointRouting>();
        using var customized = factory
            .WithWebHostBuilder(builder => factory.ConfigureWebHostCalled.Add("Customization"))
            .WithWebHostBuilder(builder => factory.ConfigureWebHostCalled.Add("FurtherCustomization"));
        var client = customized.CreateClient();

        // Assert
        Assert.Equal(new[] { "ConfigureWebHost", "Customization", "FurtherCustomization" }, factory.ConfigureWebHostCalled.ToArray());
        Assert.True(factory.CreateServerCalled);
        Assert.True(factory.CreateWebHostBuilderCalled);
        // GetTestAssemblies is not called when reading content roots from MvcAppManifest
        Assert.False(factory.GetTestAssembliesCalled);
        Assert.True(factory.CreateHostBuilderCalled);
        Assert.False(factory.CreateHostCalled);
    }

    [Fact]
    public void TestingInfrastructure_GenericHost_WithWithHostBuilderRespectsCustomizations()
    {
        // Act
        using var factory = new CustomizedFactory<GenericHostWebSite.Startup>();
        using var customized = factory
            .WithWebHostBuilder(builder => factory.ConfigureWebHostCalled.Add("Customization"))
            .WithWebHostBuilder(builder => factory.ConfigureWebHostCalled.Add("FurtherCustomization"));
        var client = customized.CreateClient();

        // Assert
        Assert.Equal(new[] { "ConfigureWebHost", "Customization", "FurtherCustomization" }, factory.ConfigureWebHostCalled.ToArray());
        Assert.False(factory.GetTestAssembliesCalled);
        Assert.True(factory.CreateHostBuilderCalled);
        Assert.True(factory.CreateHostCalled);
        Assert.False(factory.CreateServerCalled);
        Assert.False(factory.CreateWebHostBuilderCalled);
    }

    [Fact]
    public void TestingInfrastructure_GenericHost_WithWithHostBuilderHasServices()
    {
        // Act
        using var factory = new CustomizedFactory<GenericHostWebSite.Startup>();

        // Assert
        Assert.NotNull(factory.Services);
        Assert.NotNull(factory.Services.GetService(typeof(IConfiguration)));
    }

    [Fact]
    public void TestingInfrastructure_GenericHost_HostShouldStopBeforeDispose()
    {
        // Act
        using var factory = new CustomizedFactory<GenericHostWebSite.Startup>();
        var callbackCalled = false;

        var lifetimeService = (IHostApplicationLifetime)factory.Services.GetService(typeof(IHostApplicationLifetime));
        lifetimeService.ApplicationStopped.Register(() => { callbackCalled = true; });
        factory.Dispose();

        // Assert
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task TestingInfrastructure_GenericHost_HostDisposeAsync()
    {
        // Arrange
        using var factory = new CustomizedFactory<GenericHostWebSite.Startup>().WithWebHostBuilder(ConfigureWebHostBuilder);
        using var scope = factory.Services.CreateAsyncScope();
        var sink = scope.ServiceProvider.GetRequiredService<DisposableService>();

        // Act
        await scope.DisposeAsync();

        // Assert
        Assert.True(sink._asyncDisposed);
    }

    [Fact]
    public void TestingInfrastructure_GenericHost_HostDispose()
    {
        // Arrange
        using var factory = new CustomizedFactory<GenericHostWebSite.Startup>().WithWebHostBuilder(ConfigureWebHostBuilder);
        using var scope = factory.Services.CreateScope();
        var sink = scope.ServiceProvider.GetRequiredService<DisposableService>();

        // Act
        scope.Dispose();

        // Assert
        Assert.True(sink._asyncDisposed);
    }

    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<GenericHostWebSite.Startup>()
        .ConfigureServices(s => s.AddScoped<DisposableService>());

    private class DisposableService : IAsyncDisposable, IDisposable
    {
        public bool _asyncDisposed = false;
        public ValueTask DisposeAsync()
        {
            _asyncDisposed = true;
            return ValueTask.CompletedTask;
        }
        public void Dispose()
        {
            _asyncDisposed = true;
        }
    }

    private class CustomizedFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
    {
        public bool GetTestAssembliesCalled { get; private set; }
        public bool CreateWebHostBuilderCalled { get; private set; }
        public bool CreateHostBuilderCalled { get; private set; }
        public bool CreateServerCalled { get; private set; }
        public bool CreateHostCalled { get; private set; }
        public IList<string> ConfigureWebHostCalled { get; private set; } = new List<string>();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            ConfigureWebHostCalled.Add("ConfigureWebHost");
            base.ConfigureWebHost(builder);
        }

        protected override TestServer CreateServer(IWebHostBuilder builder)
        {
            CreateServerCalled = true;
            return base.CreateServer(builder);
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            CreateHostCalled = true;
            return base.CreateHost(builder);
        }

        protected override IWebHostBuilder CreateWebHostBuilder()
        {
            CreateWebHostBuilderCalled = true;
            return base.CreateWebHostBuilder();
        }

        protected override IHostBuilder CreateHostBuilder()
        {
            CreateHostBuilderCalled = true;
            return base.CreateHostBuilder();
        }

        protected override IEnumerable<Assembly> GetTestAssemblies()
        {
            GetTestAssembliesCalled = true;
            return base.GetTestAssemblies();
        }
    }
}
