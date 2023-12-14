// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Mvc.Routing;

internal readonly struct ConventionalRouteEntry
{
    public readonly RoutePattern Pattern;
    public readonly string RouteName;
    public readonly RouteValueDictionary? DataTokens;
    public readonly int Order;
    public readonly IReadOnlyList<Action<EndpointBuilder>> Conventions;
    public readonly IReadOnlyList<Action<EndpointBuilder>> FinallyConventions;

    public ConventionalRouteEntry(
        string routeName,
        string pattern,
        RouteValueDictionary? defaults,
        IDictionary<string, object?>? constraints,
        RouteValueDictionary? dataTokens,
        int order,
        List<Action<EndpointBuilder>> conventions,
        List<Action<EndpointBuilder>> finallyConventions)
    {
        RouteName = routeName;
        DataTokens = dataTokens;
        Order = order;
        Conventions = conventions;
        FinallyConventions = finallyConventions;

        try
        {
            // Data we parse from the pattern will be used to fill in the rest of the constraints or
            // defaults. The parser will throw for invalid routes.
            Pattern = RoutePatternFactory.Parse(pattern, defaults, constraints);
        }
        catch (Exception exception)
        {
            throw new RouteCreationException(string.Format(
                CultureInfo.CurrentCulture,
                "An error occurred while creating the route with name '{0}' and pattern '{1}'.",
                routeName,
                pattern), exception);
        }
    }
}
