// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Routing;

internal static class DynamicEndpointMetadataMerger
{
    /// <summary>
    /// Produces a copy of <paramref name="resolved"/> that also carries the user-configured metadata
    /// from the original dynamic endpoint. The original metadata is inserted at lower precedence than
    /// the resolved endpoint's own metadata, so page/controller-level metadata continues to win when
    /// both provide the same metadata type.
    /// </summary>
    /// <param name="resolved">The endpoint the dynamic endpoint was resolved to.</param>
    /// <param name="originalMetadata">The metadata of the original dynamic endpoint.</param>
    /// <returns>
    /// A new endpoint with the merged metadata, preserving the concrete endpoint type when possible.
    /// If there is no user-configured metadata to carry over, <paramref name="resolved"/> is returned as-is.
    /// </returns>
    public static Endpoint MergeMetadata(Endpoint resolved, IReadOnlyList<object> originalMetadata)
    {
        // Determine whether there is any metadata worth carrying over. Internal routing markers such as
        // IDynamicEndpointMetadata must never be copied onto the resolved endpoint: doing so would make
        // the resolved endpoint look dynamic and cause the matcher policy to re-process (and re-expand)
        // the already-resolved candidates within the same ApplyAsync pass.
        var mergeableCount = 0;
        for (var i = 0; i < originalMetadata.Count; i++)
        {
            if (originalMetadata[i] is not IDynamicEndpointMetadata)
            {
                mergeableCount++;
            }
        }

        if (mergeableCount == 0)
        {
            return resolved;
        }

        // Original (dynamic endpoint) metadata is less significant than the metadata discovered on
        // the resolved page/controller endpoint, so it goes first. EndpointMetadataCollection returns
        // the last matching item from GetMetadata<T>(), which means the resolved metadata takes priority.
        var merged = new List<object>(mergeableCount + resolved.Metadata.Count);
        for (var i = 0; i < originalMetadata.Count; i++)
        {
            if (originalMetadata[i] is not IDynamicEndpointMetadata)
            {
                merged.Add(originalMetadata[i]);
            }
        }
        merged.AddRange(resolved.Metadata);

        var metadata = new EndpointMetadataCollection(merged);

        if (resolved is RouteEndpoint routeEndpoint)
        {
            return new RouteEndpoint(
                routeEndpoint.RequestDelegate!,
                routeEndpoint.RoutePattern,
                routeEndpoint.Order,
                metadata,
                routeEndpoint.DisplayName);
        }

        return new Endpoint(resolved.RequestDelegate, metadata, resolved.DisplayName);
    }
}
