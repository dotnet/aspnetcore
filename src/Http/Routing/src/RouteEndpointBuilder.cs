// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Supports building a new <see cref="RouteEndpoint"/>.
/// </summary>
public sealed class RouteEndpointBuilder : EndpointBuilder
{
    // TODO: Make this public as a gettable IReadOnlyList<Func<RouteHandlerContext, RouteHandlerFilterDelegate, RouteHandlerFilterDelegate>>.
    // AddRouteHandlerFilter will still be the only way to mutate this list.
    internal List<Func<RouteHandlerContext, RouteHandlerFilterDelegate, RouteHandlerFilterDelegate>>? RouteHandlerFilterFactories { get; set; }

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
       RequestDelegate requestDelegate,
       RoutePattern routePattern,
       int order)
    {
        RequestDelegate = requestDelegate;
        RoutePattern = routePattern;
        Order = order;
    }

    /// <inheritdoc />
    [UnconditionalSuppressMessage("Trimmer", "IL2026",
        Justification = "We surface a RequireUnreferencedCode in AddRouteHandlerFilter which is required to call unreferenced code here. The trimmer is unable to infer this.")]
    public override Endpoint Build()
    {
        if (RequestDelegate is null)
        {
            throw new InvalidOperationException($"{nameof(RequestDelegate)} must be specified to construct a {nameof(RouteEndpoint)}.");
        }

        var requestDelegate = RequestDelegate;

        // Only replace the RequestDelegate if filters have been applied to this builder and they were not already handled by RouteEndpointDataSource.
        // This affects other data sources like DefaultEndpointDataSource (this is people manually newing up a data source with a list of Endpoints),
        // ModelEndpointDataSource (Map(RoutePattern, RequestDelegate) and by extension MapHub, MapHealthChecks, etc...),
        // ActionEndpointDataSourceBase (MapControllers, MapRazorPages, etc...) and people with custom data sources or otherwise manually building endpoints
        // using this type. At the moment this class is sealed, so at the moment we do not need to concern ourselves with what derived types may be doing.
        if (RouteHandlerFilterFactories is { Count: > 0 })
        {
            // Even with filters applied, RDF.Create() will return back the exact same RequestDelegate instance we pass in if filters decide not to modify the
            // invocation pipeline. We're just passing in a RequestDelegate so none of the fancy options pertaining to how the Delegate parameters are handled
            // do not matter.
            RequestDelegateFactoryOptions rdfOptions = new()
            {
                RouteHandlerFilterFactories = RouteHandlerFilterFactories,
                EndpointMetadata = Metadata,
            };

            // We ignore the returned EndpointMetadata has been already populated since we passed in non-null EndpointMetadata.
            requestDelegate = RequestDelegateFactory.Create(requestDelegate, rdfOptions).RequestDelegate;
        }

        var routeEndpoint = new RouteEndpoint(
            requestDelegate,
            RoutePattern,
            Order,
            new EndpointMetadataCollection(Metadata),
            DisplayName);

        return routeEndpoint;
    }
}
