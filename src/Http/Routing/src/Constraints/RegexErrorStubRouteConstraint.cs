// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !COMPONENTS
using Microsoft.AspNetCore.Http;
#else
using Microsoft.AspNetCore.Components.Routing;
#endif

namespace Microsoft.AspNetCore.Routing.Constraints;

#if !COMPONENTS
internal sealed class RegexErrorStubRouteConstraint : IRouteConstraint
#else
internal sealed class RegexErrorStubRouteConstraint : IRouteConstraint, IParameterPolicy
#endif
{
    public RegexErrorStubRouteConstraint(string _)
    {
        throw new InvalidOperationException(Resources.RegexRouteContraint_NotConfigured);
    }

#if !COMPONENTS
    bool IRouteConstraint.Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
#else
    bool IRouteConstraint.Match(string routeKey, RouteValueDictionary values)
#endif
    {
        // Should never get called, but is same as throw in constructor in case constructor is changed.
        throw new InvalidOperationException(Resources.RegexRouteContraint_NotConfigured);
    }
}
