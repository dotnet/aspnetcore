// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints.Infrastructure;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to configure an <see cref="IServiceCollection"/> for components.
/// </summary>
public static class ServerRazorComponentsBuilderExtensions
{
    /// <summary>
    /// Adds services to support rendering interactive server components in a razor components
    /// application.
    /// </summary>
    /// <param name="builder">The <see cref="IRazorComponentsBuilder"/>.</param>
    /// <param name="configure">A callback to configure <see cref="CircuitOptions"/>.</param>
    /// <returns>An <see cref="IRazorComponentsBuilder"/> that can be used to further customize the configuration.</returns>
    [RequiresUnreferencedCode("Server-side Blazor does not currently support native AOT.", Url = "https://aka.ms/aspnet/nativeaot")]
    public static IServerSideBlazorBuilder AddServerComponents(this IRazorComponentsBuilder builder, Action<CircuitOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        builder.Services.AddServerSideBlazor(configure);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<RenderModeEndpointProvider, CircuitEndpointProvider>());

        return new DefaultServerSideBlazorBuilder(builder.Services);
    }

    private sealed class DefaultServerSideBlazorBuilder : IServerSideBlazorBuilder
    {
        public DefaultServerSideBlazorBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }

    private class CircuitEndpointProvider : RenderModeEndpointProvider
    {
        public CircuitEndpointProvider(IServiceProvider services)
        {
            Services = services;
        }

        public IServiceProvider Services { get; }

        public override IEnumerable<RouteEndpointBuilder> GetEndpointBuilders(
            IComponentRenderMode renderMode,
            IApplicationBuilder applicationBuilder)
        {
            var endpointRouteBuilder = new EndpointRouteBuilder(Services, applicationBuilder);
            endpointRouteBuilder.MapBlazorHub();

            return endpointRouteBuilder.GetEndpoints();
        }

        public override bool Supports(IComponentRenderMode renderMode)
        {
            return renderMode switch
            {
                ServerRenderMode _ or AutoRenderMode _ => true,
                _ => false,
            };
        }

        private class EndpointRouteBuilder : IEndpointRouteBuilder
        {
            private readonly IApplicationBuilder _applicationBuilder;

            public EndpointRouteBuilder(IServiceProvider serviceProvider, IApplicationBuilder applicationBuilder)
            {
                ServiceProvider = serviceProvider;
                _applicationBuilder = applicationBuilder;
            }

            public IServiceProvider ServiceProvider { get; }

            public ICollection<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>() { };

            public IApplicationBuilder CreateApplicationBuilder()
            {
                return _applicationBuilder.New();
            }

            internal IEnumerable<RouteEndpointBuilder> GetEndpoints()
            {
                foreach (var ds in DataSources)
                {
                    foreach (var endpoint in ds.Endpoints)
                    {
                        var routeEndpoint = (RouteEndpoint)endpoint;
                        var builder = new RouteEndpointBuilder(endpoint.RequestDelegate, routeEndpoint.RoutePattern, routeEndpoint.Order);
                        for (var i = 0; i < routeEndpoint.Metadata.Count; i++)
                        {
                            var metadata = routeEndpoint.Metadata[i];
                            builder.Metadata.Add(metadata);
                        }

                        yield return builder;
                    }
                }
            }
        }
    }
}
