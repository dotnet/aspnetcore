// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Routing.Constraints;

/// <summary>
/// Constrains a route parameter to be a string with a minimum length.
/// </summary>
public class MinLengthRouteConstraint : IRouteConstraint, IParameterLiteralNodeMatchingPolicy, ICachableParameterPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MinLengthRouteConstraint" /> class.
    /// </summary>
    /// <param name="minLength">The minimum length allowed for the route parameter.</param>
    public MinLengthRouteConstraint(int minLength)
    {
        if (minLength < 0)
        {
            var errorMessage = Resources.FormatArgumentMustBeGreaterThanOrEqualTo(0);
            throw new ArgumentOutOfRangeException(nameof(minLength), minLength, errorMessage);
        }

        MinLength = minLength;
    }

    /// <summary>
    /// Gets the minimum length allowed for the route parameter.
    /// </summary>
    public int MinLength { get; private set; }

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

        if (values.TryGetValue(routeKey, out var value) && value != null)
        {
            var valueString = Convert.ToString(value, CultureInfo.InvariantCulture)!;
            return CheckConstraintCore(valueString);
        }

        return false;
    }

    private bool CheckConstraintCore(string valueString)
    {
        return valueString.Length >= MinLength;
    }

    bool IParameterLiteralNodeMatchingPolicy.MatchesLiteral(string parameterName, string literal)
    {
        return CheckConstraintCore(literal);
    }
}
