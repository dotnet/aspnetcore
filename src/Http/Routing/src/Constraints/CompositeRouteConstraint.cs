// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Routing.Constraints;

/// <summary>
/// Constrains a route by several child constraints.
/// </summary>
public class CompositeRouteConstraint : IRouteConstraint, IParameterLiteralNodeMatchingPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeRouteConstraint" /> class.
    /// </summary>
    /// <param name="constraints">The child constraints that must match for this constraint to match.</param>
    public CompositeRouteConstraint(IEnumerable<IRouteConstraint> constraints)
    {
        ArgumentNullException.ThrowIfNull(constraints);

        Constraints = constraints;
    }

    /// <summary>
    /// Gets the child constraints that must match for this constraint to match.
    /// </summary>
    public IEnumerable<IRouteConstraint> Constraints { get; private set; }

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

        foreach (var constraint in Constraints)
        {
            if (!constraint.Match(httpContext, route, routeKey, values, routeDirection))
            {
                return false;
            }
        }

        return true;
    }

    bool IParameterLiteralNodeMatchingPolicy.MatchesLiteral(string parameterName, string literal)
    {
        foreach (var constraint in Constraints)
        {
            if (constraint is IParameterLiteralNodeMatchingPolicy literalConstraint && !literalConstraint.MatchesLiteral(parameterName, literal))
            {
                return false;
            }
        }

        return true;
    }
}
