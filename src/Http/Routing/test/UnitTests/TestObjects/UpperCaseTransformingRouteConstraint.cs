// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.TestObjects;

public class UpperCaseTransformingRouteConstraint : IRouteConstraint, IOutboundParameterTransformer
{
    public bool Match(
        HttpContext httpContext,
        IRouter route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        return true;
    }

    public string TransformOutbound(object value)
    {
        return value?.ToString()?.ToUpperInvariant();
    }
}
