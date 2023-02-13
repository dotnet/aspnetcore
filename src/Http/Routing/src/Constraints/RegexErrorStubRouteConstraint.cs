// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Constraints;

internal sealed class RegexErrorStubRouteConstraint : IRouteConstraint
{
    public RegexErrorStubRouteConstraint(string _)
    {
        throw new InvalidOperationException(Resources.RegexRouteContraint_NotConfigured);
    }

    bool IRouteConstraint.Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
    {
        // Should never get called, but is same as throw in constructor in case constructor is changed.
        throw new InvalidOperationException(Resources.RegexRouteContraint_NotConfigured);
    }
}
