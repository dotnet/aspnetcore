// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Routing.Constraints;

/// <summary>
/// Constrains a route parameter to match a regular expression.
/// </summary>
public class RegexRouteConstraint : IRouteConstraint, IParameterLiteralNodeMatchingPolicy
{
    private static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(10);
    private readonly string _regexPattern;
    private Regex? _constraint;

    /// <summary>
    /// Constructor for a <see cref="RegexRouteConstraint"/> given a <paramref name="regex"/>.
    /// </summary>
    /// <param name="regex">A <see cref="Regex"/> instance to use as a constraint.</param>
    public RegexRouteConstraint(Regex regex)
    {
        ArgumentNullException.ThrowIfNull(regex);

        _constraint = regex;
        _regexPattern = regex.ToString();
    }

    /// <summary>
    /// Constructor for a <see cref="RegexRouteConstraint"/> given a <paramref name="regexPattern"/>.
    /// </summary>
    /// <param name="regexPattern">A string containing the regex pattern.</param>
    public RegexRouteConstraint(
        [StringSyntax(StringSyntaxAttribute.Regex, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        string regexPattern)
    {
        ArgumentNullException.ThrowIfNull(regexPattern);

        _regexPattern = regexPattern;
    }

    /// <summary>
    /// Gets the regular expression used in the route constraint.
    /// </summary>
    public Regex Constraint
    {
        get
        {
            // Create regex instance lazily to avoid compiling regexes at app startup. Delay creation until constraint is first evaluated.
            // This is not thread safe. No side effect but multiple instances of a regex instance could be created from a burst of requests.
            _constraint ??= new Regex(
                _regexPattern,
                RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase,
                RegexMatchTimeout);

            return _constraint;
        }
    }

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

        if (values.TryGetValue(routeKey, out var routeValue)
            && routeValue != null)
        {
            var parameterValueString = Convert.ToString(routeValue, CultureInfo.InvariantCulture)!;

            return Constraint.IsMatch(parameterValueString);
        }

        return false;
    }

    bool IParameterLiteralNodeMatchingPolicy.MatchesLiteral(string parameterName, string literal)
    {
        return Constraint.IsMatch(literal);
    }
}
