// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Routing.Constraints;

/// <summary>
/// Constrains a route parameter to represent only <see cref="DateTime"/> values.
/// </summary>
/// <remarks>
/// This constraint tries to parse strings by using all of the formats returned by the
/// CultureInfo.InvariantCulture.DateTimeFormat.GetAllDateTimePatterns() method.
/// For a sample on how to list all formats which are considered, please visit
/// http://msdn.microsoft.com/en-us/library/aszyst2c(v=vs.110).aspx
/// </remarks>
public class DateTimeRouteConstraint : IRouteConstraint, IParameterLiteralNodeMatchingPolicy, ICachableParameterPolicy
{
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
            if (value is DateTime)
            {
                return true;
            }

            var valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
            return CheckConstraintCore(valueString);
        }

        return false;
    }

    private static bool CheckConstraintCore(string? valueString)
    {
        return DateTime.TryParse(valueString, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }

    bool IParameterLiteralNodeMatchingPolicy.MatchesLiteral(string parameterName, string literal)
    {
        return CheckConstraintCore(literal);
    }
}
