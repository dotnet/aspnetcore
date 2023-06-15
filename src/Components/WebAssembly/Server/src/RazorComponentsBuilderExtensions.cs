// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to configure an <see cref="IServiceCollection"/> for WebAssembly components.
/// </summary>
public static class RazorComponentsBuilderExtensions
{
    /// <summary>
    /// Adds services to support rendering interactive WebAssembly components.
    /// </summary>
    /// <param name="builder">The <see cref="IRazorComponentsBuilder"/>.</param>
    /// <param name="configure">A callback to configure <see cref="WebAssemblyComponentsEndpointOptions"/>.</param>
    /// <returns>An <see cref="IRazorComponentsBuilder"/> that can be used to further customize the configuration.</returns>
    public static IRazorComponentsBuilder AddWebAssemblyComponents(this IRazorComponentsBuilder builder, Action<WebAssemblyComponentsEndpointOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<RenderModeEndpointProvider, WebAssemblyEndpointProvider>());

        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }

        return builder;
    }

    private class WebAssemblyEndpointProvider : RenderModeEndpointProvider
    {
        private readonly IServiceProvider _services;
        private readonly WebAssemblyComponentsEndpointOptions _options;

        public WebAssemblyEndpointProvider(IServiceProvider services, IOptions<WebAssemblyComponentsEndpointOptions> options)
        {
            _services = services;
            _options = options.Value;
        }

        public override IEnumerable<RouteEndpointBuilder> GetEndpointBuilders(IComponentRenderMode renderMode, IApplicationBuilder applicationBuilder)
        {
            var endpointRouteBuilder = new EndpointRouteBuilder(_services, applicationBuilder);
            var pathPrefix = _options.PathPrefix;

            applicationBuilder.UseBlazorFrameworkFiles(pathPrefix);
            var app = applicationBuilder.Build();

            endpointRouteBuilder.Map($"{pathPrefix}/_framework/{{*path}}", context =>
            {
                // Set endpoint to null so the static files middleware will handle the request.
                context.SetEndpoint(null);

                return app(context);
            });

            return endpointRouteBuilder.GetEndpoints();
        }

        public override bool Supports(IComponentRenderMode renderMode)
            => renderMode is WebAssemblyRenderMode or AutoRenderMode;

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
