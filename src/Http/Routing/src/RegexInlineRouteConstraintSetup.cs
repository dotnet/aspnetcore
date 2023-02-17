// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class RegexInlineRouteConstraintSetup : IConfigureOptions<RouteOptions>
{
    public void Configure(RouteOptions options)
    {
        var existingRegexConstraintType = options.TrimmerSafeConstraintMap["regex"];

        // Don't override regex constraint if it has already been overridden
        // this behavior here is just to add it back in if someone calls AddRouting(...)
        // after setting up routing with AddRoutingCore(...).
        if (existingRegexConstraintType == typeof(RegexErrorStubRouteConstraint))
        {
            options.SetParameterPolicy<RegexInlineRouteConstraint>("regex");
        }
    }
}
