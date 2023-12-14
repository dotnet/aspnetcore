// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Routing.Constraints;

/// <summary>
/// Constrains a route parameter to contain only a specified string.
/// </summary>
public class StringRouteConstraint : IRouteConstraint, IParameterLiteralNodeMatchingPolicy, ICachableParameterPolicy
{
    private readonly string _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="StringRouteConstraint"/> class.
    /// </summary>
    /// <param name="value">The constraint value to match.</param>
    public StringRouteConstraint(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _value = value;
    }

    /// <inheritdoc />
    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
    {
        ArgumentNullException.ThrowIfNull(routeKey);
        ArgumentNullException.ThrowIfNull(values);

        if (values.TryGetValue(routeKey, out var routeValue)
            && routeValue != null)
        {
            var parameterValueString = Convert.ToString(routeValue, CultureInfo.InvariantCulture)!;
            return CheckConstraintCore(parameterValueString);
        }

        return false;
    }

    private bool CheckConstraintCore(string parameterValueString)
    {
        return parameterValueString.Equals(_value, StringComparison.OrdinalIgnoreCase);
    }

    bool IParameterLiteralNodeMatchingPolicy.MatchesLiteral(string parameterName, string literal)
    {
        return CheckConstraintCore(literal);
    }
}
