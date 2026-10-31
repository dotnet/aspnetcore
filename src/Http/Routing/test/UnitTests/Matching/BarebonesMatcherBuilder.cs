// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Patterns;
using static Microsoft.AspNetCore.Routing.Matching.BarebonesMatcher;

namespace Microsoft.AspNetCore.Routing.Matching;

internal class BarebonesMatcherBuilder : MatcherBuilder
{
    private readonly List<RouteEndpoint> _endpoints = new List<RouteEndpoint>();

    public override void AddEndpoint(RouteEndpoint endpoint)
    {
        _endpoints.Add(endpoint);
    }

    public override Matcher Build()
    {
        var matchers = new InnerMatcher[_endpoints.Count];
        for (var i = 0; i < _endpoints.Count; i++)
        {
            var endpoint = _endpoints[i];
            var pathSegments = endpoint.RoutePattern.PathSegments
                .Select(s => s.IsSimple && s.Parts[0] is RoutePatternLiteralPart literalPart ? literalPart.Content : null)
                .ToArray();
            matchers[i] = new InnerMatcher(pathSegments, _endpoints[i]);
        }

        return new BarebonesMatcher(matchers);
    }
}
