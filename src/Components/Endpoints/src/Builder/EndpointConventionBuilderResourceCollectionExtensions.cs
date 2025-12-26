// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for <see cref="IEndpointConventionBuilder"/> to add resource collection metadata.
/// </summary>
public static class EndpointConventionBuilderResourceCollectionExtensions
{
    /// <summary>
    /// Provides a helper to attach ResourceCollection metadata to endpoints.
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
    /// <param name="manifestPath">The manifest associated with the assets.</param>
    /// <returns>The <see cref="IEndpointConventionBuilder"/> that can be used to further configure the endpoints.</returns>
    /// <remarks>
    /// This method attaches static asset metadata to endpoints. It provides a simplified way to add
    /// resource collection metadata to any endpoint convention builder.
    /// The <paramref name="manifestPath"/> must match the path of the manifest file provided to
    /// the <see cref="StaticAssetsEndpointRouteBuilderExtensions.MapStaticAssets(IEndpointRouteBuilder, string?)"/> call.
    /// </remarks>
    public static IEndpointConventionBuilder WithStaticAssets(
        this IEndpointConventionBuilder builder,
        string? manifestPath = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Only add the convention if the builder also implements IEndpointRouteBuilder
        // (e.g., RouteGroupBuilder). For other builders like RouteHandlerBuilder,
        // the convention won't be able to resolve static assets.
        if (builder is IEndpointRouteBuilder routeBuilder)
        {
            ResourceCollectionConvention convention = new ResourceCollectionConvention(routeBuilder, manifestPath);
            builder.Add(convention.Apply);
        }

        return builder;
    }

    private sealed class ResourceCollectionConvention
    {
        private readonly IEndpointRouteBuilder _routeBuilder;
        private readonly string? _manifestPath;
        private ResourceAssetCollection? _collection;
        private ResourcePreloadCollection? _preloadCollection;
        private ImportMapDefinition? _importMap;

        public ResourceCollectionConvention(IEndpointRouteBuilder routeBuilder, string? manifestPath)
        {
            _routeBuilder = routeBuilder;
            _manifestPath = manifestPath;
        }

        public void Apply(EndpointBuilder endpointBuilder)
        {
            // Check if there's already a resource collection on the metadata
            if (endpointBuilder.Metadata.OfType<ResourceAssetCollection>().Any())
            {
                return;
            }

            if (_collection == null)
            {
                // We only use the resolver to get to the datasources so we can cache the results for
                // all endpoints in the collection
                var resolver = new ResourceCollectionResolver(_routeBuilder);

                if (resolver.IsRegistered(_manifestPath))
                {
                    _collection = resolver.ResolveResourceCollection(_manifestPath);
                    _preloadCollection = new ResourcePreloadCollection(_collection);
                    _importMap = ImportMapDefinition.FromResourceCollection(_collection);
                }
            }

            if (_collection != null)
            {
                endpointBuilder.Metadata.Add(_collection);
                endpointBuilder.Metadata.Add(_preloadCollection!);
                endpointBuilder.Metadata.Add(_importMap!);
            }
        }
    }
}