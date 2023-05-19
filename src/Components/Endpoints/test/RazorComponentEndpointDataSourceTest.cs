// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Discovery;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class RazorComponentEndpointDataSourceTest
{
    [Fact]
    public void RegistersEndpoints()
    {
        var endpointDataSource = CreateDataSource<App>();

        var endpoints = endpointDataSource.Endpoints;
        Assert.Equal(2, endpoints.Count);
    }

    [Fact]
    public void ServerModeWiresUpServerEndpoints()
    {
        var builder = CreateBuilder(typeof(ServerComponent));
        var services = CreateServices(typeof(ServerEndpointProvider));
        var endpointDataSource = CreateDataSource<App>(builder, services);

        var endpoints = endpointDataSource.Endpoints;
        var endpoint = Assert.Single(endpoints);

        Assert.Equal("/server", ((RouteEndpoint)endpoint).RoutePattern.RawText);
    }

    [Fact]
    public void ServerModeNoProviderThrows()
    {

        var builder = CreateBuilder(typeof(ServerComponent));
        var services = CreateServices();
        var endpointDataSource = CreateDataSource<App>(builder, services);

        Assert.Throws<InvalidOperationException>(() => endpointDataSource.Endpoints);
    }

    [Fact]
    public void WebAssemblyWiresUpWebAssemblyEndpoints()
    {

        var builder = CreateBuilder(typeof(WebAssemblyComponent));
        var services = CreateServices(typeof(WebassemblyEndpointProvider));
        var endpointDataSource = CreateDataSource<App>(builder, services);

        var endpoints = endpointDataSource.Endpoints;
        var endpoint = Assert.Single(endpoints);

        Assert.Equal("/webassembly", ((RouteEndpoint)endpoint).RoutePattern.RawText);
    }

    [Fact]
    public void WebAssemblyNoProviderThrows()
    {

        var builder = CreateBuilder(typeof(WebAssemblyComponent));
        var services = CreateServices();
        var endpointDataSource = CreateDataSource<App>(builder, services);

        Assert.Throws<InvalidOperationException>(() => endpointDataSource.Endpoints);
    }

    [Fact]
    public void AutoWiresUpWebAssemblyEndpointsWhenOnlyWebAssemblyIsConfigured()
    {

        var builder = CreateBuilder(typeof(AutoComponent));
        var services = CreateServices(typeof(WebassemblyEndpointProvider));
        var endpointDataSource = CreateDataSource<App>(builder, services);

        var endpoints = endpointDataSource.Endpoints;
        var endpoint = Assert.Single(endpoints);

        Assert.Equal("/webassembly", ((RouteEndpoint)endpoint).RoutePattern.RawText);
    }

    [Fact]
    public void AutoWiresUpServerEndpointsWhenOnlyServerIsConfigured()
    {

        var builder = CreateBuilder(typeof(AutoComponent));
        var services = CreateServices(typeof(ServerEndpointProvider));
        var endpointDataSource = CreateDataSource<App>(builder, services);

        var endpoints = endpointDataSource.Endpoints;
        var endpoint = Assert.Single(endpoints);

        Assert.Equal("/server", ((RouteEndpoint)endpoint).RoutePattern.RawText);
    }

    [Fact]
    public void AutoWiresUpServerAndWebAssemblyEndpointsWhenBothAreConfigured()
    {

        var builder = CreateBuilder(typeof(AutoComponent));
        var services = CreateServices(typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider));
        var endpointDataSource = CreateDataSource<App>(builder, services);

        var endpoints = endpointDataSource.Endpoints;

        Assert.Collection(endpoints,
            endpoint => Assert.Equal("/server", ((RouteEndpoint)endpoint).RoutePattern.RawText),
            endpoint => Assert.Equal("/webassembly", ((RouteEndpoint)endpoint).RoutePattern.RawText));
    }

    [Fact]
    public void AutoNoProviderThrows()
    {

        var builder = CreateBuilder(typeof(AutoComponent));
        var services = CreateServices();
        var endpointDataSource = CreateDataSource<App>(builder, services);

        Assert.Throws<InvalidOperationException>(() => endpointDataSource.Endpoints);
    }

    [Fact]
    public void NoDiscoveredModesDefaultsToStatic()
    {

        var builder = CreateBuilder();
        var services = CreateServices(typeof(ServerEndpointProvider));
        var endpointDataSource = CreateDataSource<App>(builder, services);

        var endpoints = endpointDataSource.Endpoints;
        Assert.Empty(endpoints);
    }

    [Theory]
    [MemberData(nameof(SetRenderModes))]
    public void ExplicitlySetRenderModesAreRespected(
        IComponentRenderMode[] renderModes,
        Type[] providers,
        Type[] components,
        string[] expectedEndpoints)
    {
        var builder = CreateBuilder(components);
        var services = CreateServices(providers);
        var endpointDataSource = CreateDataSource<App>(builder, services, renderModes);

        var endpoints = endpointDataSource.Endpoints;

        switch (expectedEndpoints.Length)
        {
            case 0:
                Assert.Empty(endpoints);
                break;
            case 1:
                var endpoint = Assert.Single(endpoints);
                Assert.Equal(expectedEndpoints[0], ((RouteEndpoint)endpoint).RoutePattern.RawText);
                break;
            case 2:
                Assert.Collection(endpoints,
                    endpoint => Assert.Equal(expectedEndpoints[0], ((RouteEndpoint)endpoint).RoutePattern.RawText),
                    endpoint => Assert.Equal(expectedEndpoints[1], ((RouteEndpoint)endpoint).RoutePattern.RawText));
                break;
            default:
                Assert.Fail("Too many endpoints specified");
                break;
        }
    }

    public static TheoryData<IComponentRenderMode[], Type[], Type[], string[]> SetRenderModes =>
        new()
        {
            {
                new []  { RenderMode.Server },
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(AutoComponent) },
                new []  { "/server" }
            },
            {
                new []  { RenderMode.Server },
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new [] { typeof(ServerComponent) },
                new []  { "/server" }
            },
            {
                new []  { RenderMode.Server },
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(WebAssemblyComponent) },
                new []  { "/server" }
            },
            {
                new []  { RenderMode.WebAssembly },
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(AutoComponent) },
                new []  { "/webassembly" }
            },
            {
                new []  { RenderMode.WebAssembly },
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(ServerComponent) },
                new []  { "/webassembly" }
            },
            {
                new []  { RenderMode.WebAssembly },
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(WebAssemblyComponent) },
                new []  { "/webassembly" }
            },
            {
                Array.Empty<IComponentRenderMode>(),
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(AutoComponent) },
                Array.Empty<string>()
            },
            {
                Array.Empty<IComponentRenderMode>(),
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(ServerComponent) },
                Array.Empty<string>()
            },
            {
                Array.Empty<IComponentRenderMode>(),
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(WebAssemblyComponent) },
                Array.Empty<string>()
            }
        };


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

    private RazorComponentEndpointDataSource<TComponent> CreateDataSource<TComponent>(
        ComponentApplicationBuilder builder = null,
        IServiceProvider services = null,
        IComponentRenderMode[] renderModes = null)
    {
        var result = new RazorComponentEndpointDataSource<TComponent>(
            builder ?? DefaultRazorComponentApplication<TComponent>.Instance.GetBuilder(),
            services?.GetService<IEnumerable<RenderModeEndpointProvider>>() ?? Enumerable.Empty<RenderModeEndpointProvider>(),
            new ApplicationBuilder(services ?? new ServiceCollection().BuildServiceProvider()),
            new RazorComponentEndpointFactory());

        if (builder == null || renderModes != null)
        {
            result.DefaultBuilder.SetRenderModes(renderModes ?? Array.Empty<IComponentRenderMode>());
        }

        return result;
    }

    private class StaticComponent : ComponentBase { }

    [RenderModeServer]
    private class ServerComponent : ComponentBase { }

    [RenderModeAuto]
    private class AutoComponent : ComponentBase { }

    [RenderModeWebAssembly]
    private class WebAssemblyComponent : ComponentBase { }

    private class ServerEndpointProvider : RenderModeEndpointProvider
    {
        public override IEnumerable<RouteEndpointBuilder> GetEndpointBuilders(IComponentRenderMode renderMode, IApplicationBuilder applicationBuilder)
        {
            yield return new RouteEndpointBuilder(
                (context) => Task.CompletedTask,
                RoutePatternFactory.Parse("/server"),
                0);
        }

        public override bool Supports(IComponentRenderMode renderMode) => renderMode is ServerRenderMode or AutoRenderMode;
    }

    private class WebassemblyEndpointProvider : RenderModeEndpointProvider
    {
        public override IEnumerable<RouteEndpointBuilder> GetEndpointBuilders(IComponentRenderMode renderMode, IApplicationBuilder applicationBuilder)
        {
            yield return new RouteEndpointBuilder(
                (context) => Task.CompletedTask,
                RoutePatternFactory.Parse("/webassembly"),
                0);
        }

        public override bool Supports(IComponentRenderMode renderMode) => renderMode is WebAssemblyRenderMode or AutoRenderMode;
    }
}

public class App : IComponent
{
    public void Attach(RenderHandle renderHandle)
    {
        throw new NotImplementedException();
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        throw new NotImplementedException();
    }
}

[Route("/")]
public class Index : IComponent
{
    public void Attach(RenderHandle renderHandle)
    {
        throw new NotImplementedException();
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        throw new NotImplementedException();
    }
}

[Route("/counter")]
public class Counter : IComponent
{
    public void Attach(RenderHandle renderHandle)
    {
        throw new NotImplementedException();
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        throw new NotImplementedException();
    }
}
