// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace RoutingSandbox.Framework;

internal class FrameworkEndpointDataSource : EndpointDataSource, IEndpointConventionBuilder
{
    private readonly RoutePatternTransformer _routePatternTransformer;
    private readonly List<Action<EndpointBuilder>> _conventions;

    public List<RoutePattern> Patterns { get; }
    public List<HubMethod> HubMethods { get; }

    private List<Endpoint> _endpoints;

    public FrameworkEndpointDataSource(RoutePatternTransformer routePatternTransformer)
    {
        _routePatternTransformer = routePatternTransformer;
        _conventions = new List<Action<EndpointBuilder>>();

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

                var endpointBuilder = new RouteEndpointBuilder(
                    hubMethod.RequestDelegate,
                    resolvedPattern,
                    order++);
                endpointBuilder.DisplayName = $"{hubMethod.Hub}.{hubMethod.Method}";

                foreach (var convention in _conventions)
                {
                    convention(endpointBuilder);
                }

                endpoints.Add(endpointBuilder.Build());
            }
        }

        return endpoints;
    }

    public override IChangeToken GetChangeToken()
    {
        return NullChangeToken.Singleton;
    }

    public void Add(Action<EndpointBuilder> convention)
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
