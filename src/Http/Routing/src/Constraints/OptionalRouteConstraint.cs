// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Constraints;

/// <summary>
/// Defines a constraint on an optional parameter. If the parameter is present, then it is constrained by InnerConstraint.
/// </summary>
public class OptionalRouteConstraint : IRouteConstraint
{
    /// <summary>
    /// Creates a new <see cref="OptionalRouteConstraint"/> instance given the <paramref name="innerConstraint"/>.
    /// </summary>
    /// <param name="innerConstraint"></param>
    public OptionalRouteConstraint(IRouteConstraint innerConstraint)
    {
        ArgumentNullException.ThrowIfNull(innerConstraint);

        InnerConstraint = innerConstraint;
    }

    /// <summary>
    /// Gets the <see cref="IRouteConstraint"/> associated with the optional parameter.
    /// </summary>
    public IRouteConstraint InnerConstraint { get; }

    /// <inheritdoc />
    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        ArgumentNullException.ThrowIfNull(routeKey);
        ArgumentNullException.ThrowIfNull(values);

        if (values.TryGetValue(routeKey, out _))
        {
            return InnerConstraint.Match(httpContext,
                                         route,
                                         routeKey,
                                         values,
                                         routeDirection);
        }

        return true;
    }
}
