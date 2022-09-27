// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Template;

public class RoutePatternPrecedenceTests : RoutePrecedenceTestsBase
{
    protected override decimal ComputeMatched(string template)
    {
        return ComputeRoutePattern(template, RoutePrecedence.ComputeInbound);
    }

    protected override decimal ComputeGenerated(string template)
    {
        return ComputeRoutePattern(template, RoutePrecedence.ComputeOutbound);
    }

    private static decimal ComputeRoutePattern(string template, Func<RoutePattern, decimal> func)
    {
        var parsed = RoutePatternFactory.Parse(template);
        return func(parsed);
    }

    [Fact]
    public void InboundPrecedence_ParameterWithRequiredValue_HasPrecedence()
    {
        var parameterPrecedence = RoutePatternFactory.Parse(
            "{controller}").InboundPrecedence;

        var requiredValueParameterPrecedence = RoutePatternFactory.Parse(
            "{controller}",
            defaults: null,
            parameterPolicies: null,
            requiredValues: new { controller = "Home" }).InboundPrecedence;

        Assert.True(requiredValueParameterPrecedence < parameterPrecedence);
    }
}
