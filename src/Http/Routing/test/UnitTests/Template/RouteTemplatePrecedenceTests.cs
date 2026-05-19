// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Template;

public class RouteTemplatePrecedenceTests : RoutePrecedenceTestsBase
{
    protected override decimal ComputeMatched(string template)
    {
        return ComputeRouteTemplate(template, RoutePrecedence.ComputeInbound);
    }

    protected override decimal ComputeGenerated(string template)
    {
        return ComputeRouteTemplate(template, RoutePrecedence.ComputeOutbound);
    }

    private static decimal ComputeRouteTemplate(string template, Func<RouteTemplate, decimal> func)
    {
        var parsed = TemplateParser.Parse(template);
        return func(parsed);
    }
}
