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

        // Early return if builder doesn't implement IEndpointRouteBuilder
        if (builder is not IEndpointRouteBuilder routeBuilder)
        {
            return builder;
        }

        // Create resolver once outside the lambda
        var resolver = new ResourceCollectionResolver(routeBuilder);

        // Resolve collection and related metadata once if registered
        ResourceAssetCollection? collection = null;
        ResourcePreloadCollection? preloadCollection = null;
        ImportMapDefinition? importMap = null;

        if (resolver.IsRegistered(manifestPath))
        {
            collection = resolver.ResolveResourceCollection(manifestPath);
            preloadCollection = new ResourcePreloadCollection(collection);
            importMap = ImportMapDefinition.FromResourceCollection(collection);
        }

        builder.Add(endpointBuilder =>
        {
            // Early return if collection is not available
            if (collection == null)
            {
                return;
            }

            // Check if there's already a resource collection on the metadata
            if (endpointBuilder.Metadata.OfType<ResourceAssetCollection>().Any())
            {
                return;
            }

            endpointBuilder.Metadata.Add(collection);
            endpointBuilder.Metadata.Add(preloadCollection!);
            endpointBuilder.Metadata.Add(importMap!);
        });
        
        return builder;
    }
}