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
        var endpointDataSource = CreateDataSource<App>(useDeclaredRenderModes: false);

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
    [MemberData(nameof(ConfiguredAndDiscoveredRenderModes))]
    public void RenderModesAreConfiguredCorrectly(
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
                Assert.Collection(endpoints.OrderBy(e => ((RouteEndpoint)e).RoutePattern.RawText),
                    endpoint => Assert.Equal(expectedEndpoints[0], ((RouteEndpoint)endpoint).RoutePattern.RawText),
                    endpoint => Assert.Equal(expectedEndpoints[1], ((RouteEndpoint)endpoint).RoutePattern.RawText));
                break;
            default:
                Assert.Fail("Too many endpoints specified");
                break;
        }
    }

    [Theory]
    [MemberData(nameof(SetRenderModesFailures))]
    public void ThrowsForDeclaredRenderModes_WhenMissingRenderModes(
        IComponentRenderMode[] renderModes,
        Type[] providers,
        Type[] components)
    {
        var builder = CreateBuilder(components);
        var services = CreateServices(providers);
        var endpointDataSource = CreateDataSource<App>(builder, services, renderModes);

        Assert.Throws<InvalidOperationException>(() => endpointDataSource.Endpoints);
    }

    // renderModes, providers, components, expectedEndpoints
    public static TheoryData<IComponentRenderMode[], Type[], Type[], string[]> ConfiguredAndDiscoveredRenderModes =>
        new()
        {
            // Auto component sets up server and wasm when available
            {
                Array.Empty<IComponentRenderMode>(),
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(AutoComponent) },
                new []  { "/server", "/webassembly" }
            },
            // Auto component adds webassembly when available
            {
                new []  { RenderMode.Server },
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(AutoComponent) },
                new []  { "/server", "/webassembly" }
            },
            // Auto component adds server when available
            {
                new []  { RenderMode.WebAssembly },
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(AutoComponent) },
                new []  { "/server", "/webassembly" }
            },
            // Auto component does nothing because modes are explicitly configured
            {
                new IComponentRenderMode []  { RenderMode.Server, RenderMode.WebAssembly },
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(AutoComponent) },
                new []  { "/server", "/webassembly" }
            },
            // Webassembly component wires up webassembly endpoints
            {
                Array.Empty<IComponentRenderMode>(),
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(WebAssemblyComponent) },
                new []  { "/webassembly" }
            },
            // Webassembly component wires up webassembly endpoints in addition to server endpoints
            // as they were explicitly configured.
            {
                new []  { RenderMode.Server },
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(WebAssemblyComponent) },
                new []  { "/server", "/webassembly" }
            },
            // Webassembly component does nothing as webassembly is already configured explicitly.
            {
                new []  { RenderMode.WebAssembly },
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(WebAssemblyComponent) },
                new []  { "/webassembly" }
            },
            // Server and webassembly endpoints are added as they were explicitly configured.
            {
                new IComponentRenderMode [] { RenderMode.Server, RenderMode.WebAssembly },
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(WebAssemblyComponent) },
                new []  { "/server", "/webassembly" }
            },
            // Server component wires up server components
            {
                Array.Empty<IComponentRenderMode>(),
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(ServerComponent) },
                new []  { "/server" }
            },
            // Server component does nothing as server is already configured.
            {
                new []  { RenderMode.Server },
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(ServerComponent) },
                new []  { "/server" }
            },
            // Server component wires up server endpoints in addition to webassembly endpoints
            // that were explicitly configured.
            {
                new []  { RenderMode.WebAssembly },
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(ServerComponent) },
                new []  { "/server", "/webassembly" }
            },
            // Server component does nothing as server and webassembly endpoints were explicitly configured.
            {
                new IComponentRenderMode [] { RenderMode.Server, RenderMode.WebAssembly },
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(ServerComponent) },
                new []  { "/server", "/webassembly" }
            },
        };

    // renderModes, providers, components, expectedEndpoints
    public static TheoryData<IComponentRenderMode[], Type[], Type[]> SetRenderModesFailures =>
        new()
        {
            // Missing server and webassembly, auto requires both.
            {
                Array.Empty<IComponentRenderMode>(),
                Array.Empty<Type>(),
                new []  { typeof(AutoComponent) }
            },
            // Missing webassembly, auto requires it.
            {
                Array.Empty<IComponentRenderMode>(),
                new []  { typeof(ServerEndpointProvider) },
                new []  { typeof(AutoComponent) }
            },
            // Missing server, auto requires it.
            {
                Array.Empty<IComponentRenderMode>(),
                new []  { typeof(WebassemblyEndpointProvider) },
                new []  { typeof(AutoComponent) }
            },

            // Missing server.
            {
                Array.Empty<IComponentRenderMode>(),
                Array.Empty<Type>(),
                new []  { typeof(ServerComponent) }
            },
            // Missing server.
            {
                Array.Empty<IComponentRenderMode>(),
                new []  { typeof(WebassemblyEndpointProvider) },
                new []  { typeof(ServerComponent) }
            },
            // Server explicitly configured and missing
            {
                new IComponentRenderMode[] { RenderMode.Server },
                new []  { typeof(WebassemblyEndpointProvider) },
                Array.Empty<Type>()
            },

            // Missing webassembly
            {
                Array.Empty<IComponentRenderMode>(),
                Array.Empty<Type>(),
                new []  { typeof(WebAssemblyComponent) }
            },

            // Missing webassembly
            {
                Array.Empty<IComponentRenderMode>(),
                new []  { typeof(ServerEndpointProvider) },
                new []  { typeof(WebAssemblyComponent) }
            },

            // Webassembly explicitly configured and missing
            {
                new IComponentRenderMode[] { RenderMode.WebAssembly },
                new []  { typeof(ServerEndpointProvider) },
                Array.Empty<Type>()
            },
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
        IComponentRenderMode[] renderModes = null,
        bool useDeclaredRenderModes = true)
    {
        var result = new RazorComponentEndpointDataSource<TComponent>(
            builder ?? DefaultRazorComponentApplication<TComponent>.Instance.GetBuilder(),
            services?.GetService<IEnumerable<RenderModeEndpointProvider>>() ?? Enumerable.Empty<RenderModeEndpointProvider>(),
            new ApplicationBuilder(services ?? new ServiceCollection().BuildServiceProvider()),
            new RazorComponentEndpointFactory());

        result.Options.UseDeclaredRenderModes = useDeclaredRenderModes;

        if (renderModes != null)
        {
            foreach (var mode in renderModes)
            {
                result.Options.ConfiguredRenderModes.Add(mode);
            }
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
