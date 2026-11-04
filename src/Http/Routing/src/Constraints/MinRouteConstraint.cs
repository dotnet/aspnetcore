// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
#if !COMPONENTS
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;
#else
using Microsoft.AspNetCore.Components.Routing;
#endif

namespace Microsoft.AspNetCore.Routing.Constraints;

#if !COMPONENTS
/// <summary>
/// Constrains a route parameter to be a long with a minimum value.
/// </summary>
public class MinRouteConstraint : IRouteConstraint, IParameterLiteralNodeMatchingPolicy, ICachableParameterPolicy
#else
internal class MinRouteConstraint : IRouteConstraint
#endif
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MinRouteConstraint" /> class.
    /// </summary>
    /// <param name="min">The minimum value allowed for the route parameter.</param>
    public MinRouteConstraint(long min)
    {
        Min = min;
    }

    /// <summary>
    /// Gets the minimum allowed value of the route parameter.
    /// </summary>
    public long Min { get; }

    /// <inheritdoc />
    public bool Match(
#if !COMPONENTS
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
#else
        string routeKey,
        RouteValueDictionary values)
#endif
    {
        ArgumentNullException.ThrowIfNull(routeKey);
        ArgumentNullException.ThrowIfNull(values);

        if (values.TryGetValue(routeKey, out var value) && value != null)
        {
            var valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
            return CheckConstraintCore(valueString);
        }

        return false;
    }

    private bool CheckConstraintCore(string? valueString)
    {
        if (long.TryParse(valueString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
        {
            return longValue >= Min;
        }
        return false;
    }

#if !COMPONENTS
    bool IParameterLiteralNodeMatchingPolicy.MatchesLiteral(string parameterName, string literal)
    {
        return CheckConstraintCore(literal);
    }
#endif
}
