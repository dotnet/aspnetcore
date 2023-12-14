// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !COMPONENTS
using Microsoft.AspNetCore.Http;
#else
using Microsoft.AspNetCore.Components.Routing;
#endif

namespace Microsoft.AspNetCore.Routing.Constraints;

internal sealed class NullRouteConstraint : IRouteConstraint
{
    public static readonly NullRouteConstraint Instance = new NullRouteConstraint();

    private NullRouteConstraint()
    {
    }

#if !COMPONENTS
    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
#else
    public bool Match(string routeKey, RouteValueDictionary values)
#endif
    {
        return true;
    }
}
