// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

internal sealed class PageActionEndpointDataSource : ActionEndpointDataSourceBase
{
    private readonly ActionEndpointFactory _endpointFactory;
    private readonly OrderedEndpointsSequenceProvider _orderSequence;

    public PageActionEndpointDataSource(
        PageActionEndpointDataSourceIdProvider dataSourceIdProvider,
        IActionDescriptorCollectionProvider actions,
        ActionEndpointFactory endpointFactory,
        OrderedEndpointsSequenceProvider orderedEndpoints)
        : base(actions)
    {
        DataSourceId = dataSourceIdProvider.CreateId();
        _endpointFactory = endpointFactory;
        _orderSequence = orderedEndpoints;
        DefaultBuilder = new PageActionEndpointConventionBuilder(Lock, Conventions, FinallyConventions);

        // IMPORTANT: this needs to be the last thing we do in the constructor.
        // Change notifications can happen immediately!
        Subscribe();
    }

    public int DataSourceId { get; }

    public PageActionEndpointConventionBuilder DefaultBuilder { get; }

    // Used to control whether we create 'inert' (non-routable) endpoints for use in dynamic
    // selection. Set to true by builder methods that do dynamic/fallback selection.
    public bool CreateInertEndpoints { get; set; }

    protected override List<Endpoint> CreateEndpoints(
        RoutePattern? groupPrefix,
        IReadOnlyList<ActionDescriptor> actions,
        IReadOnlyList<Action<EndpointBuilder>> conventions,
        IReadOnlyList<Action<EndpointBuilder>> groupConventions,
        IReadOnlyList<Action<EndpointBuilder>> finallyConventions,
        IReadOnlyList<Action<EndpointBuilder>> groupFinallyConventions)
    {
        var endpoints = new List<Endpoint>();
        var routeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < actions.Count; i++)
        {
            if (actions[i] is PageActionDescriptor action)
            {
                _endpointFactory.AddEndpoints(endpoints,
                    routeNames,
                    action,
                    Array.Empty<ConventionalRouteEntry>(),
                    conventions: conventions,
                    groupConventions: groupConventions,
                    finallyConventions: finallyConventions,
                    groupFinallyConventions: groupFinallyConventions,
                    CreateInertEndpoints,
                    groupPrefix);
            }
        }

        return endpoints;
    }

    internal void AddDynamicPageEndpoint(IEndpointRouteBuilder endpoints, string pattern, Type transformerType, object? state, int? order = null)
    {
        CreateInertEndpoints = true;
        lock (Lock)
        {
            order ??= _orderSequence.GetNext();

            endpoints.Map(
                pattern,
                context =>
                {
                    throw new InvalidOperationException("This endpoint is not expected to be executed directly.");
                })
                .Add(b =>
                {
                    ((RouteEndpointBuilder)b).Order = order.Value;
                    b.Metadata.Add(new DynamicPageRouteValueTransformerMetadata(transformerType, state));
                    b.Metadata.Add(new PageEndpointDataSourceIdMetadata(DataSourceId));
                });
        }
    }
}
