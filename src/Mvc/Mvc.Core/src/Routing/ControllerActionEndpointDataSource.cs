// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal class ControllerActionEndpointDataSource : ActionEndpointDataSourceBase
    {
        private readonly ActionEndpointFactory _endpointFactory;
        private readonly List<ConventionalRouteEntry> _routes;

        private int _order;

        public ControllerActionEndpointDataSource(
            IActionDescriptorCollectionProvider actions,
            ActionEndpointFactory endpointFactory)
            : base(actions)
        {
            _endpointFactory = endpointFactory;

            _routes = new List<ConventionalRouteEntry>();

            // In traditional conventional routing setup, the routes defined by a user have a order
            // defined by how they are added into the list. We would like to maintain the same order when building
            // up the endpoints too.
            //
            // Start with an order of '1' for conventional routes as attribute routes have a default order of '0'.
            // This is for scenarios dealing with migrating existing Router based code to Endpoint Routing world.
            _order = 1;

            DefaultBuilder = new ControllerActionEndpointConventionBuilder(Lock, Conventions);

            // IMPORTANT: this needs to be the last thing we do in the constructor.
            // Change notifications can happen immediately!
            Subscribe();
        }

        public ControllerActionEndpointConventionBuilder DefaultBuilder { get; }

        // Used to control whether we create 'inert' (non-routable) endpoints for use in dynamic
        // selection. Set to true by builder methods that do dynamic/fallback selection.
        public bool CreateInertEndpoints { get; set; }

        public ControllerActionEndpointConventionBuilder AddRoute(
            string routeName,
            string pattern,
            RouteValueDictionary defaults,
            IDictionary<string, object> constraints,
            RouteValueDictionary dataTokens)
        {
            lock (Lock)
            {
                var conventions = new List<Action<EndpointBuilder>>();
                _routes.Add(new ConventionalRouteEntry(routeName, pattern, defaults, constraints, dataTokens, _order++, conventions));
                return new ControllerActionEndpointConventionBuilder(Lock, conventions);
            }
        }

        protected override List<Endpoint> CreateEndpoints(IReadOnlyList<ActionDescriptor> actions, IReadOnlyList<Action<EndpointBuilder>> conventions)
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
                    _endpointFactory.AddEndpoints(endpoints, routeNames, action, _routes, conventions, CreateInertEndpoints);

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
                _endpointFactory.AddConventionalLinkGenerationRoute(endpoints, routeNames, keys, route, conventions);
            }

            return endpoints;
        }
    }
}

