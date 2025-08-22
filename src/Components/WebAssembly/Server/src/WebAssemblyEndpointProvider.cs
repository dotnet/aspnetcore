// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.Extensions.DependencyInjection;

internal class WebAssemblyEndpointProvider(IServiceProvider services) : RenderModeEndpointProvider
{
    private const string ResourceCollectionKey = "__ResourceCollectionKey";

    public override IEnumerable<RouteEndpointBuilder> GetEndpointBuilders(IComponentRenderMode renderMode, IApplicationBuilder applicationBuilder)
    {
        if (renderMode is not WebAssemblyRenderModeWithOptions wasmWithOptions)
        {
            return renderMode is InteractiveWebAssemblyRenderMode
                ? throw new InvalidOperationException("Invalid render mode. Use AddInteractiveWebAssemblyRenderMode(Action<WebAssemblyComponentsEndpointOptions>) to configure the WebAssembly render mode.")
                : [];
        }
        if (applicationBuilder.Properties[ResourceCollectionKey] is ResourceAssetCollection assetMap)
        {
            return [];
        }
        else
        {
            // In case the app didn't call MapStaticAssets, use the 8.0 approach to map the assets.
            var endpointRouteBuilder = new WebAssemblyEndpointRouteBuilder(services, applicationBuilder);
            var pathPrefix = wasmWithOptions.EndpointOptions?.PathPrefix;

            applicationBuilder.UseBlazorFrameworkFiles(pathPrefix ?? default);
            var app = applicationBuilder.Build();

            endpointRouteBuilder.Map($"{pathPrefix}/_framework/{{*path}}", context =>
            {
                // Set endpoint to null so the static files middleware will handle the request.
                context.SetEndpoint(null);

                return app(context);
            });

            return endpointRouteBuilder.GetEndpoints();
        }
    }

    public override bool Supports(IComponentRenderMode renderMode) =>
        renderMode is InteractiveWebAssemblyRenderMode or InteractiveAutoRenderMode;

    private class WebAssemblyEndpointRouteBuilder(IServiceProvider serviceProvider, IApplicationBuilder applicationBuilder) : IEndpointRouteBuilder
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;

        public ICollection<EndpointDataSource> DataSources { get; } = [];

        public IApplicationBuilder CreateApplicationBuilder()
        {
            return applicationBuilder.New();
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
