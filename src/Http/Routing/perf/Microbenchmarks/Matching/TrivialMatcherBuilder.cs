// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Matching;

internal sealed class TrivialMatcherBuilder : MatcherBuilder
{
    private readonly List<RouteEndpoint> _endpoints = new List<RouteEndpoint>();

    public override void AddEndpoint(RouteEndpoint endpoint)
    {
        _endpoints.Add(endpoint);
    }

    public override Matcher Build()
    {
        return new TrivialMatcher(_endpoints.Last());
    }
}
