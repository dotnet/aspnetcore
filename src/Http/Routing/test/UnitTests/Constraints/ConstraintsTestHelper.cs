// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Tests;

public class ConstraintsTestHelper
{
    public static bool TestConstraint(IRouteConstraint constraint, object value)
    {
        var parameterName = "fake";
        var values = new RouteValueDictionary() { { parameterName, value } };
        var routeDirection = RouteDirection.IncomingRequest;
        return constraint.Match(httpContext: null, route: null, parameterName, values, routeDirection);
    }
}
