// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

// This is an adapter to use Route in the conformance tests
internal class RouteMatcher : Matcher
{
    private readonly RouteCollection _inner;

    internal RouteMatcher(RouteCollection inner)
    {
        _inner = inner;
    }

    public override async Task MatchAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var routeContext = new RouteContext(httpContext);
        await _inner.RouteAsync(routeContext);

        if (routeContext.Handler != null)
        {
            httpContext.Request.RouteValues = routeContext.RouteData.Values;
            await routeContext.Handler(httpContext);
        }
    }
}

