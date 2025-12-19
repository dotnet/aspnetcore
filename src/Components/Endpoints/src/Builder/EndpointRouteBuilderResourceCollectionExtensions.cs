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

        builder.Add(endpointBuilder =>
        {
            // Check if there's already a resource collection on the metadata
            if (endpointBuilder.Metadata.OfType<ResourceAssetCollection>().Any())
            {
                return;
            }

            // Check if the builder is also an IEndpointRouteBuilder
            if (builder is IEndpointRouteBuilder routeBuilder)
            {
                var resolver = new ResourceCollectionResolver(routeBuilder);
                
                // Only add metadata if static assets are registered
                if (resolver.IsRegistered(manifestPath))
                {
                    var collection = resolver.ResolveResourceCollection(manifestPath);
                    var importMap = ImportMapDefinition.FromResourceCollection(collection);

                    endpointBuilder.Metadata.Add(collection);
                    endpointBuilder.Metadata.Add(new ResourcePreloadCollection(collection));
                    endpointBuilder.Metadata.Add(importMap);
                }
            }
        });
        
        return builder;
    }
}