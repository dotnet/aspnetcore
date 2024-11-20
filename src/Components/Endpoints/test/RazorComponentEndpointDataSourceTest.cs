// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Discovery;
using Microsoft.AspNetCore.Components.Endpoints.Infrastructure;
using Microsoft.AspNetCore.Components.Endpoints.Tests;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class RazorComponentEndpointDataSourceTest
{
    [Fact]
    public void RegistersEndpoints()
    {
        var endpointDataSource = CreateDataSource<App>();

        var endpoints = endpointDataSource.Endpoints;

        Assert.Equal(3, endpoints.Count);
    }

    [Fact]
    public void NoDiscoveredModesDefaultsToStatic()
    {

        var builder = CreateBuilder();
        var services = CreateServices(typeof(ServerEndpointProvider));
        var endpointDataSource = CreateDataSource<App>(builder, services);

        var endpoints = endpointDataSource.Endpoints;

        Assert.Single(endpoints);
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
                new []  { RenderMode.InteractiveServer },
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(AutoComponent) },
                new []  { "/server", "/webassembly" }
            },
            // Auto component adds server when available
            {
                new []  { RenderMode.InteractiveWebAssembly },
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(AutoComponent) },
                new []  { "/server", "/webassembly" }
            },
            // Auto component does nothing because modes are explicitly configured
            {
                new IComponentRenderMode []  { RenderMode.InteractiveServer, RenderMode.InteractiveWebAssembly },
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
                new []  { RenderMode.InteractiveServer },
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(WebAssemblyComponent) },
                new []  { "/server", "/webassembly" }
            },
            // Webassembly component does nothing as webassembly is already configured explicitly.
            {
                new []  { RenderMode.InteractiveWebAssembly },
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(WebAssemblyComponent) },
                new []  { "/webassembly" }
            },
            // Server and webassembly endpoints are added as they were explicitly configured.
            {
                new IComponentRenderMode [] { RenderMode.InteractiveServer, RenderMode.InteractiveWebAssembly },
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
                new []  { RenderMode.InteractiveServer },
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(ServerComponent) },
                new []  { "/server" }
            },
            // Server component wires up server endpoints in addition to webassembly endpoints
            // that were explicitly configured.
            {
                new []  { RenderMode.InteractiveWebAssembly },
                new []  { typeof(ServerEndpointProvider), typeof(WebassemblyEndpointProvider) },
                new []  { typeof(ServerComponent) },
                new []  { "/server", "/webassembly" }
            },
            // Server component does nothing as server and webassembly endpoints were explicitly configured.
            {
                new IComponentRenderMode [] { RenderMode.InteractiveServer, RenderMode.InteractiveWebAssembly },
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
                new IComponentRenderMode[] { RenderMode.InteractiveServer },
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
                new IComponentRenderMode[] { RenderMode.InteractiveWebAssembly },
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

        services.AddSingleton<IWebHostEnvironment, TestWebHostEnvironment>();
        services.AddLogging();

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
            new TestEndpointRouteBuilder(services ?? CreateServices()),
            new RazorComponentEndpointFactory(),
            new HotReloadService() { MetadataUpdateSupported = true });

        if (renderModes != null)
        {
            foreach (var mode in renderModes)
            {
                result.Options.ConfiguredRenderModes.Add(mode);
            }
        }

        return result;
    }

    [TestRenderMode<InteractiveServerRenderMode>]
    private class ServerComponent : ComponentBase { }

    [TestRenderMode<InteractiveAutoRenderMode>]
    private class AutoComponent : ComponentBase { }

    [TestRenderMode<InteractiveWebAssemblyRenderMode>]
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

        public override bool Supports(IComponentRenderMode renderMode) => renderMode is InteractiveServerRenderMode or InteractiveAutoRenderMode;
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

        public override bool Supports(IComponentRenderMode renderMode) => renderMode is InteractiveWebAssemblyRenderMode or InteractiveAutoRenderMode;
    }

    private class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "TestApplication";
        public string EnvironmentName { get; set; } = "TestEnvironment";
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get => ContentRootFileProvider; set { } }
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = CreateTestFileProvider();

        private static TestFileProvider CreateTestFileProvider()
        {
            var provider = new TestFileProvider();
            provider.AddFile("site.css", "body { color: red; }");
            return provider;
        }
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
