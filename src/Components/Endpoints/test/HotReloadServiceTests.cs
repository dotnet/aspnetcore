// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using System.Reflection;
using Microsoft.AspNetCore.Components.Discovery;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Components.Endpoints.Infrastructure;
using Microsoft.AspNetCore.Components.Infrastructure;

namespace Microsoft.AspNetCore.Components.Endpoints.Tests;

public class HotReloadServiceTests
{
    [Fact]
    public void UpdatesEndpointsWhenHotReloadChangeTokenTriggered()
    {
        // Arrange
        var builder = CreateBuilder(typeof(ServerComponent));
        var services = CreateServices(typeof(MockEndpointProvider));
        var endpointDataSource = CreateDataSource<App>(builder, services);
        var invoked = false;

        // Act
        ChangeToken.OnChange(endpointDataSource.GetChangeToken, () => invoked = true);

        // Assert
        Assert.False(invoked);
        HotReloadService.UpdateApplication(null);
        Assert.True(invoked);
    }

    [Fact]
    public void AddNewEndpointWhenDataSourceChanges()
    {
        // Arrange
        var builder = CreateBuilder(typeof(ServerComponent));
        var services = CreateServices(typeof(MockEndpointProvider));
        var endpointDataSource = CreateDataSource<App>(builder, services);

        // Assert - 1
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(endpointDataSource.Endpoints));
        Assert.Equal("/server", endpoint.RoutePattern.RawText);

        // Act - 2
        endpointDataSource.Builder.Pages.AddFromLibraryInfo("TestAssembly2", new[]
        {
            new PageComponentBuilder
            {
                AssemblyName = "TestAssembly2",
                PageType = typeof(StaticComponent),
                RouteTemplates = new List<string> { "/app/test" }
            }
        });
        HotReloadService.UpdateApplication(null);

        // Assert - 2
        Assert.Equal(2, endpointDataSource.Endpoints.Count);
        Assert.Collection(
            endpointDataSource.Endpoints,
            (ep) => Assert.Equal("/app/test", ((RouteEndpoint)ep).RoutePattern.RawText),
            (ep) => Assert.Equal("/server", ((RouteEndpoint)ep).RoutePattern.RawText));
    }

    [Fact]
    public void RemovesEndpointWhenDataSourceChanges()
    {
        // Arrange
        var builder = CreateBuilder(typeof(ServerComponent));
        var services = CreateServices(typeof(MockEndpointProvider));
        var endpointDataSource = CreateDataSource<App>(builder, services);

        // Assert - 1
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(endpointDataSource.Endpoints));
        Assert.Equal("/server", endpoint.RoutePattern.RawText);

        // Act - 2
        endpointDataSource.Builder.RemoveLibrary("TestAssembly");
        endpointDataSource.Options.ConfiguredRenderModes.Clear();
        HotReloadService.UpdateApplication(null);

        // Assert - 2
        Assert.Empty(endpointDataSource.Endpoints);
    }

    [Fact]
    public void ModifiesEndpointWhenDataSourceChanges()
    {
        // Arrange
        var builder = CreateBuilder(typeof(ServerComponent));
        var services = CreateServices(typeof(MockEndpointProvider));
        var endpointDataSource = CreateDataSource<App>(builder, services);

        // Assert - 1
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(endpointDataSource.Endpoints));
        Assert.Equal("/server", endpoint.RoutePattern.RawText);
        Assert.DoesNotContain(endpoint.Metadata, (element) => element is TestMetadata);

        // Act - 2
        endpointDataSource.Conventions.Add(builder =>
            builder.Metadata.Add(new TestMetadata()));
        HotReloadService.UpdateApplication(null);

        // Assert - 2
        var updatedEndpoint = Assert.IsType<RouteEndpoint>(Assert.Single(endpointDataSource.Endpoints));
        Assert.Equal("/server", updatedEndpoint.RoutePattern.RawText);
        Assert.Contains(updatedEndpoint.Metadata, (element) => element is TestMetadata);
    }

    [Fact]
    public void NotifiesCompositeEndpointDataSource()
    {
        // Arrange
        var builder = CreateBuilder(typeof(ServerComponent));
        var services = CreateServices(typeof(MockEndpointProvider));
        var endpointDataSource = CreateDataSource<App>(builder, services);
        var compositeEndpointDataSource = new CompositeEndpointDataSource(
            new[] { endpointDataSource });

        // Assert - 1
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(endpointDataSource.Endpoints));
        Assert.Equal("/server", endpoint.RoutePattern.RawText);
        var compositeEndpoint = Assert.IsType<RouteEndpoint>(Assert.Single(compositeEndpointDataSource.Endpoints));
        Assert.Equal("/server", compositeEndpoint.RoutePattern.RawText);

        // Act - 2
        endpointDataSource.Builder.Pages.RemoveFromAssembly("TestAssembly");
        endpointDataSource.Options.ConfiguredRenderModes.Clear();
        HotReloadService.UpdateApplication(null);

        // Assert - 2
        Assert.Empty(endpointDataSource.Endpoints);
        Assert.Empty(compositeEndpointDataSource.Endpoints);
    }

    private sealed class WrappedChangeTokenDisposable : IDisposable
    {
        public bool IsDisposed { get; private set; }
        private readonly IDisposable _innerDisposable;

        public WrappedChangeTokenDisposable(IDisposable innerDisposable)
        { 
            _innerDisposable = innerDisposable;
        }

        public void Dispose()
        { 
             IsDisposed = true;
             _innerDisposable.Dispose();
        }
    }

    [Fact]
    public void ConfirmChangeTokenDisposedHotReload()
    {
        // Arrange
        var builder = CreateBuilder(typeof(ServerComponent));
        var services = CreateServices(typeof(MockEndpointProvider));
        var endpointDataSource = CreateDataSource<App>(builder, services);

        WrappedChangeTokenDisposable wrappedChangeTokenDisposable = null;

        endpointDataSource.SetDisposableChangeTokenAction = (IDisposable disposableChangeToken) => {
            wrappedChangeTokenDisposable = new WrappedChangeTokenDisposable(disposableChangeToken); 
            return wrappedChangeTokenDisposable;
        };

        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(endpointDataSource.Endpoints));
        Assert.Equal("/server", endpoint.RoutePattern.RawText);
        Assert.DoesNotContain(endpoint.Metadata, (element) => element is TestMetadata);

        // Make a modification and then perform a hot reload.
        endpointDataSource.Conventions.Add(builder =>
            builder.Metadata.Add(new TestMetadata()));
        HotReloadService.UpdateApplication(null);
        HotReloadService.ClearCache(null);

        // Confirm the change token is disposed after ClearCache
        Assert.True(wrappedChangeTokenDisposable.IsDisposed);
    }

    private class TestMetadata { }

    private ComponentApplicationBuilder CreateBuilder(params Type[] types)
    {
        var builder = new ComponentApplicationBuilder();
        builder.AddLibrary(new AssemblyComponentLibraryDescriptor(
            "TestAssembly",
            Array.Empty<PageComponentBuilder>(),
            types.Select(t => new ComponentBuilder
            {
                AssemblyName = "TestAssembly",
                ComponentType = t,
                RenderMode = t.GetCustomAttribute<RenderModeAttribute>()
            }).ToArray()));

        return builder;
    }

    private IServiceProvider CreateServices(params Type[] types)
    {
        var services = new ServiceCollection();
        foreach (var type in types)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(RenderModeEndpointProvider), type));
        }

        return services.BuildServiceProvider();
    }

    private static RazorComponentEndpointDataSource<TComponent> CreateDataSource<TComponent>(
        ComponentApplicationBuilder builder,
        IServiceProvider services,
        IComponentRenderMode[] renderModes = null)
    {
        var result = new RazorComponentEndpointDataSource<TComponent>(
            builder,
            new[] { new MockEndpointProvider() },
            new TestEndpointRouteBuilder(services),
            new RazorComponentEndpointFactory(),
            new HotReloadService() { MetadataUpdateSupported = true });

        if (renderModes != null)
        {
            foreach (var mode in renderModes)
            {
                result.Options.ConfiguredRenderModes.Add(mode);
            }
        }
        else
        {
            result.Options.ConfiguredRenderModes.Add(new InteractiveServerRenderMode());
        }

        return result;
    }

    private class StaticComponent : ComponentBase { }

    [TestRenderMode<InteractiveServerRenderMode>]
    private class ServerComponent : ComponentBase { }

    private class MockEndpointProvider : RenderModeEndpointProvider
    {
        public override IEnumerable<RouteEndpointBuilder> GetEndpointBuilders(IComponentRenderMode renderMode, IApplicationBuilder applicationBuilder)
        {
            yield return new RouteEndpointBuilder(
                (context) => Task.CompletedTask,
                RoutePatternFactory.Parse("/server"),
                0);
        }

        public override bool Supports(IComponentRenderMode renderMode) => true;
    }

    private class TestEndpointRouteBuilder : IEndpointRouteBuilder
    {
        private IServiceProvider _serviceProvider;
        private List<EndpointDataSource> _dataSources = new();

        public TestEndpointRouteBuilder(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public IServiceProvider ServiceProvider => _serviceProvider;

        public ICollection<EndpointDataSource> DataSources => _dataSources;

        public IApplicationBuilder CreateApplicationBuilder() => new ApplicationBuilder(_serviceProvider);
    }
}
