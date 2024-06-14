// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for <see cref="PageActionEndpointConventionBuilder"/>.
/// </summary>
public static class PageActionEndpointConventionBuilderResourceCollectionExtensions
{
    /// <summary>
    /// Adds a <see cref="ResourceAssetCollection"/> metadata instance to the endpoints.
    /// </summary>
    /// <param name="builder">The <see cref="PageActionEndpointConventionBuilder"/>.</param>
    /// <param name="manifestPath">The manifest associated with the assets.</param>
    /// <returns></returns>
    public static PageActionEndpointConventionBuilder WithStaticAssets(
        this PageActionEndpointConventionBuilder builder,
        string? manifestPath = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var endpointBuilder = builder.Items["__EndpointRouteBuilder"];
        var (resolver, registered) = builder.Items.TryGetValue("__ResourceCollectionResolver", out var value)
            ? ((ResourceCollectionResolver)value, true)
            : (new ResourceCollectionResolver((IEndpointRouteBuilder)endpointBuilder), false);

        resolver.ManifestName = manifestPath;
        if (!registered)
        {
            var collection = resolver.ResolveResourceCollection();
            var importMap = resolver.ResolveImportMap();

            builder.Add(endpointBuilder =>
            {
                endpointBuilder.Metadata.Add(collection);
                endpointBuilder.Metadata.Add(importMap);
            });
        }

        return builder;
    }
}
