// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Mvc.Routing;

internal sealed class ControllerActionEndpointDataSource : ActionEndpointDataSourceBase
{
    private readonly ActionEndpointFactory _endpointFactory;
    private readonly OrderedEndpointsSequenceProvider _orderSequence;
    private readonly List<ConventionalRouteEntry> _routes;

    public ControllerActionEndpointDataSource(
        ControllerActionEndpointDataSourceIdProvider dataSourceIdProvider,
        IActionDescriptorCollectionProvider actions,
        ActionEndpointFactory endpointFactory,
        OrderedEndpointsSequenceProvider orderSequence)
        : base(actions)
    {
        _endpointFactory = endpointFactory;

        DataSourceId = dataSourceIdProvider.CreateId();
        _orderSequence = orderSequence;

        _routes = new List<ConventionalRouteEntry>();

        DefaultBuilder = new ControllerActionEndpointConventionBuilder(Lock, Conventions, FinallyConventions);

        // IMPORTANT: this needs to be the last thing we do in the constructor.
        // Change notifications can happen immediately!
        Subscribe();
    }

    public int DataSourceId { get; }

    public ControllerActionEndpointConventionBuilder DefaultBuilder { get; }

    // Used to control whether we create 'inert' (non-routable) endpoints for use in dynamic
    // selection. Set to true by builder methods that do dynamic/fallback selection.
    public bool CreateInertEndpoints { get; set; }

    public ControllerActionEndpointConventionBuilder AddRoute(
        string routeName,
        string pattern,
        RouteValueDictionary? defaults,
        IDictionary<string, object?>? constraints,
        RouteValueDictionary? dataTokens)
    {
        lock (Lock)
        {
            var conventions = new List<Action<EndpointBuilder>>();
            var finallyConventions = new List<Action<EndpointBuilder>>();
            _routes.Add(new ConventionalRouteEntry(routeName, pattern, defaults, constraints, dataTokens, _orderSequence.GetNext(), conventions, finallyConventions));
            return new ControllerActionEndpointConventionBuilder(Lock, conventions, finallyConventions);
        }
    }

    protected override List<Endpoint> CreateEndpoints(
        RoutePattern? groupPrefix,
        IReadOnlyList<ActionDescriptor> actions,
        IReadOnlyList<Action<EndpointBuilder>> conventions,
        IReadOnlyList<Action<EndpointBuilder>> groupConventions,
        IReadOnlyList<Action<EndpointBuilder>> finallyConventions,
        IReadOnlyList<Action<EndpointBuilder>> groupFinallyConventions)
    {
        var endpoints = new List<Endpoint>();
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // MVC guarantees that when two of it's endpoints have the same route name they are equivalent.
        //
        // However, Endpoint Routing requires Endpoint Names to be unique.
        var routeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // For each controller action - add the relevant endpoints.
        //
        // 1. If the action is attribute routed, we use that information verbatim
        // 2. If the action is conventional routed
        //      a. Create a *matching only* endpoint for each action X route (if possible)
        //      b. Ignore link generation for now
        for (var i = 0; i < actions.Count; i++)
        {
            if (actions[i] is ControllerActionDescriptor action)
            {
                _endpointFactory.AddEndpoints(endpoints,
                                              routeNames,
                                              action,
                                              _routes,
                                              conventions: conventions,
                                              groupConventions: groupConventions,
                                              finallyConventions: finallyConventions,
                                              groupFinallyConventions: groupFinallyConventions,
                                              CreateInertEndpoints,
                                              groupPrefix: groupPrefix);

                if (_routes.Count > 0)
                {
                    // If we have conventional routes, keep track of the keys so we can create
                    // the link generation routes later.
                    foreach (var kvp in action.RouteValues)
                    {
                        keys.Add(kvp.Key);
                    }
                }
            }
        }

        // Now create a *link generation only* endpoint for each route. This gives us a very
        // compatible experience to previous versions.
        for (var i = 0; i < _routes.Count; i++)
        {
            var route = _routes[i];
            _endpointFactory.AddConventionalLinkGenerationRoute(
                endpoints,
                routeNames,
                keys,
                route,
                groupConventions: groupConventions,
                conventions: conventions,
                finallyConventions: finallyConventions,
                groupFinallyConventions: groupFinallyConventions,
                groupPrefix: groupPrefix);
        }

        return endpoints;
    }

    internal void AddDynamicControllerEndpoint(IEndpointRouteBuilder endpoints, string pattern, Type transformerType, object? state, int? order = null)
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
                    b.Metadata.Add(new DynamicControllerRouteValueTransformerMetadata(transformerType, state));
                    b.Metadata.Add(new ControllerEndpointDataSourceIdMetadata(DataSourceId));
                });
        }
    }
}

