// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Supports building a new <see cref="RouteEndpoint"/>.
/// </summary>
public sealed class RouteEndpointBuilder : EndpointBuilder
{
    /// <summary>
    /// Gets or sets the <see cref="RoutePattern"/> associated with this endpoint.
    /// </summary>
    public RoutePattern RoutePattern { get; set; }

    /// <summary>
    /// Gets or sets the order assigned to the endpoint.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Constructs a new <see cref="RouteEndpointBuilder"/> instance.
    /// </summary>
    /// <param name="requestDelegate">The delegate used to process requests for the endpoint.</param>
    /// <param name="routePattern">The <see cref="RoutePattern"/> to use in URL matching.</param>
    /// <param name="order">The order assigned to the endpoint.</param>
    public RouteEndpointBuilder(
       RequestDelegate? requestDelegate,
       RoutePattern routePattern,
       int order)
    {
        ArgumentNullException.ThrowIfNull(routePattern);

        RequestDelegate = requestDelegate;
        RoutePattern = routePattern;
        Order = order;
    }

    /// <inheritdoc />
    public override Endpoint Build()
    {
        if (RequestDelegate is null)
        {
            throw new InvalidOperationException($"{nameof(RequestDelegate)} must be specified to construct a {nameof(RouteEndpoint)}.");
        }

        return new RouteEndpoint(
            RequestDelegate,
            RoutePattern,
            Order,
            CreateMetadataCollection(Metadata, RoutePattern),
            DisplayName);
    }

    private static EndpointMetadataCollection CreateMetadataCollection(IList<object> metadata, RoutePattern routePattern)
    {
        var hasRouteDiagnosticsMetadata = false;

        if (metadata.Count > 0)
        {
            var hasCorsMetadata = false;
            IHttpMethodMetadata? httpMethodMetadata = null;

            // Before create the final collection we
            // need to update the IHttpMethodMetadata if
            // a CORS metadata is present
            for (var i = 0; i < metadata.Count; i++)
            {
                // Not using else if since a metadata could have both
                // interfaces.

                if (metadata[i] is IHttpMethodMetadata methodMetadata)
                {
                    // Storing only the last entry
                    // since the last metadata is the most significant.
                    httpMethodMetadata = methodMetadata;
                }

                if (!hasCorsMetadata && metadata[i] is ICorsMetadata)
                {
                    // IEnableCorsAttribute, IDisableCorsAttribute and ICorsPolicyMetadata
                    // are ICorsMetadata
                    hasCorsMetadata = true;
                }

                if (!hasRouteDiagnosticsMetadata && metadata[i] is IRouteDiagnosticsMetadata)
                {
                    hasRouteDiagnosticsMetadata = true;
                }
            }

            if (hasCorsMetadata && httpMethodMetadata is not null && !httpMethodMetadata.AcceptCorsPreflight)
            {
                // Since we found a CORS metadata we will update it
                // to make sure the acceptCorsPreflight is set to true.
                httpMethodMetadata.AcceptCorsPreflight = true;
            }
        }

        // No route diagnostics metadata provided so automatically add one based on the route pattern string.
        if (!hasRouteDiagnosticsMetadata)
        {
            metadata.Add(new RouteDiagnosticsMetadata(routePattern.DebuggerToString()));
        }

        return new EndpointMetadataCollection(metadata);
    }

    [DebuggerDisplay("{ToString(),nq}")]
    private sealed class RouteDiagnosticsMetadata : IRouteDiagnosticsMetadata
    {
        public string Route { get; }

        public RouteDiagnosticsMetadata(string route)
        {
            Route = route;
        }

        public override string ToString()
        {
            return DebuggerHelpers.GetDebugText(nameof(Route), Route);
        }
    }
}
