// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace RoutingSandbox.Framework
{
    internal class FrameworkEndpointDataSource : EndpointDataSource, IEndpointConventionBuilder
    {
        private readonly RoutePatternTransformer _routePatternTransformer;
        private readonly List<Action<EndpointModel>> _conventions;

        public List<RoutePattern> Patterns { get; }
        public List<HubMethod> HubMethods { get; }

        private List<Endpoint> _endpoints;

        public FrameworkEndpointDataSource(RoutePatternTransformer routePatternTransformer)
        {
            _routePatternTransformer = routePatternTransformer;
            _conventions = new List<Action<EndpointModel>>();

            Patterns = new List<RoutePattern>();
            HubMethods = new List<HubMethod>();
        }

        public override IReadOnlyList<Endpoint> Endpoints
        {
            get
            {
                if (_endpoints == null)
                {
                    _endpoints = BuildEndpoints();
                }

                return _endpoints;
            }
        }

        private List<Endpoint> BuildEndpoints()
        {
            List<Endpoint> endpoints = new List<Endpoint>();

            foreach (var hubMethod in HubMethods)
            {
                var requiredValues = new { hub = hubMethod.Hub, method = hubMethod.Method };
                var order = 1;

                foreach (var pattern in Patterns)
                {
                    var resolvedPattern = _routePatternTransformer.SubstituteRequiredValues(pattern, requiredValues);
                    if (resolvedPattern == null)
                    {
                        continue;
                    }

                    var endpointModel = new RouteEndpointModel(
                        hubMethod.RequestDelegate,
                        resolvedPattern,
                        order++);
                    endpointModel.DisplayName = $"{hubMethod.Hub}.{hubMethod.Method}";

                    foreach (var convention in _conventions)
                    {
                        convention(endpointModel);
                    }

                    endpoints.Add(endpointModel.Build());
                }
            }

            return endpoints;
        }

        public override IChangeToken GetChangeToken()
        {
            return NullChangeToken.Singleton;
        }

        public void Apply(Action<EndpointModel> convention)
        {
            _conventions.Add(convention);
        }
    }

    internal class HubMethod
    {
        public string Hub { get; set; }
        public string Method { get; set; }
        public RequestDelegate RequestDelegate { get; set; }
    }
}
