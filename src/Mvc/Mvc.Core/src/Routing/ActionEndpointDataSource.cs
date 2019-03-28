// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal class ActionEndpointDataSource : ActionEndpointDataSourceBase
    {
        private readonly ActionEndpointFactory _endpointFactory;
        private readonly List<ConventionalRouteEntry> _routes;

        public ActionEndpointDataSource(IActionDescriptorCollectionProvider actions, ActionEndpointFactory endpointFactory)
            : base(actions)
        {
            _endpointFactory = endpointFactory;

            _routes = new List<ConventionalRouteEntry>();

            // IMPORTANT: this needs to be the last thing we do in the constructor.
            // Change notifications can happen immediately!
            Subscribe();
        }

        // For testing
        public IReadOnlyList<ConventionalRouteEntry> Routes
        {
            get
            {
                lock (Lock)
                {
                    return _routes.ToArray();
                }
            }
        }

        public void AddRoute(in ConventionalRouteEntry route)
        {
            lock (Lock)
            {
                _routes.Add(route);
            }
        }

        protected override List<Endpoint> CreateEndpoints(IReadOnlyList<ActionDescriptor> actions, IReadOnlyList<Action<EndpointBuilder>> conventions)
        {
            var endpoints = new List<Endpoint>();
            for (var i = 0; i < actions.Count; i++)
            {
                _endpointFactory.AddEndpoints(endpoints, actions[i], _routes, conventions);
            }

            return endpoints;
        }
    }
}

