// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
#if !COMPONENTS
using System.Linq;
#endif
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Template;

#if !COMPONENTS
/// <summary>
/// Computes precedence for a route template.
/// </summary>
public static class RoutePrecedence
#else
internal static class RoutePrecedence
#endif
{
#if !COMPONENTS
    /// <summary>
    ///  Compute the precedence for matching a provided url
    /// </summary>
    /// <example>
    ///     e.g.: /api/template == 1.1
    ///     /api/template/{id} == 1.13
    ///     /api/{id:int} == 1.2
    ///     /api/template/{id:int} == 1.12
    /// </example>
    /// <param name="template">The <see cref="RouteTemplate"/> to compute precedence for.</param>
    /// <returns>A <see cref="decimal"/> representing the route's precedence.</returns>
    public static decimal ComputeInbound(RouteTemplate template)
    {
        ValidateSegementLength(template.Segments.Count);

        // Each precedence digit corresponds to one decimal place. For example, 3 segments with precedences 2, 1,
        // and 4 results in a combined precedence of 2.14 (decimal).
        var precedence = 0m;

        for (var i = 0; i < template.Segments.Count; i++)
        {
            var segment = template.Segments[i];

            var digit = ComputeInboundPrecedenceDigit(segment);
            Debug.Assert(digit >= 0 && digit < 10);

            precedence += decimal.Divide(digit, (decimal)Math.Pow(10, i));
        }

        return precedence;
    }
#endif

    // See description on ComputeInbound(RouteTemplate)
    internal static decimal ComputeInbound(RoutePattern routePattern)
    {
        ValidateSegementLength(routePattern.PathSegments.Count);

        var precedence = 0m;

        for (var i = 0; i < routePattern.PathSegments.Count; i++)
        {
            var segment = routePattern.PathSegments[i];

            var digit = ComputeInboundPrecedenceDigit(routePattern, segment);
            Debug.Assert(digit >= 0 && digit < 10);

            precedence += decimal.Divide(digit, (decimal)Math.Pow(10, i));
        }

        return precedence;
    }

#if !COMPONENTS
    /// <summary>
    ///  Compute the precedence for generating a url.
    /// </summary>
    /// <example>
    ///     e.g.: /api/template    == 5.5
    ///     /api/template/{id}     == 5.53
    ///     /api/{id:int}          == 5.4
    ///     /api/template/{id:int} == 5.54
    /// </example>
    /// <param name="template">The <see cref="RouteTemplate"/> to compute precedence for.</param>
    /// <returns>A <see cref="decimal"/> representing the route's precedence.</returns>
    public static decimal ComputeOutbound(RouteTemplate template)
    {
        ValidateSegementLength(template.Segments.Count);

        // Each precedence digit corresponds to one decimal place. For example, 3 segments with precedences 2, 1,
        // and 4 results in a combined precedence of 2.14 (decimal).
        var precedence = 0m;

        for (var i = 0; i < template.Segments.Count; i++)
        {
            var segment = template.Segments[i];

            var digit = ComputeOutboundPrecedenceDigit(segment);
            Debug.Assert(digit >= 0 && digit < 10);

            precedence += decimal.Divide(digit, (decimal)Math.Pow(10, i));
        }

        return precedence;
    }
#endif

    // see description on ComputeOutbound(RouteTemplate)
    internal static decimal ComputeOutbound(RoutePattern routePattern)
    {
        ValidateSegementLength(routePattern.PathSegments.Count);

        // Each precedence digit corresponds to one decimal place. For example, 3 segments with precedences 2, 1,
        // and 4 results in a combined precedence of 2.14 (decimal).
        var precedence = 0m;

        for (var i = 0; i < routePattern.PathSegments.Count; i++)
        {
            var segment = routePattern.PathSegments[i];

            var digit = ComputeOutboundPrecedenceDigit(segment);
            Debug.Assert(digit >= 0 && digit < 10);

            precedence += decimal.Divide(digit, (decimal)Math.Pow(10, i));
        }

        return precedence;
    }

    private static void ValidateSegementLength(int length)
    {
        if (length > 28)
        {
            // An OverflowException will be thrown by Math.Pow when greater than 28
            throw new InvalidOperationException("Route exceeds the maximum number of allowed segments of 28 and is unable to be processed.");
        }
    }

#if !COMPONENTS
    // Segments have the following order:
    // 5 - Literal segments
    // 4 - Multi-part segments && Constrained parameter segments
    // 3 - Unconstrained parameter segements
    // 2 - Constrained wildcard parameter segments
    // 1 - Unconstrained wildcard parameter segments
    private static int ComputeOutboundPrecedenceDigit(TemplateSegment segment)
    {
        if (segment.Parts.Count > 1)
        {
            return 4;
        }

        var part = segment.Parts[0];
        if (part.IsLiteral)
        {
            return 5;
        }
        else
        {
            Debug.Assert(part.IsParameter);
            var digit = part.IsCatchAll ? 1 : 3;

            if (part.InlineConstraints != null && part.InlineConstraints.Any())
            {
                digit++;
            }

            return digit;
        }
    }
#endif

    // See description on ComputeOutboundPrecedenceDigit(TemplateSegment segment)
    private static int ComputeOutboundPrecedenceDigit(RoutePatternPathSegment pathSegment)
    {
        if (pathSegment.Parts.Count > 1)
        {
            return 4;
        }

        var part = pathSegment.Parts[0];
        if (part.IsLiteral)
        {
            return 5;
        }
        else if (part is RoutePatternParameterPart parameterPart)
        {
            Debug.Assert(parameterPart != null);
            var digit = parameterPart.IsCatchAll ? 1 : 3;

            if (parameterPart.ParameterPolicies.Count > 0)
            {
                digit++;
            }

            return digit;
        }
        else
        {
            // Unreachable
            throw new NotSupportedException();
        }
    }

#if !COMPONENTS
    // Segments have the following order:
    // 1 - Literal segments
    // 2 - Constrained parameter segments / Multi-part segments
    // 3 - Unconstrained parameter segments
    // 4 - Constrained wildcard parameter segments
    // 5 - Unconstrained wildcard parameter segments
    private static int ComputeInboundPrecedenceDigit(TemplateSegment segment)
    {
        if (segment.Parts.Count > 1)
        {
            // Multi-part segments should appear after literal segments and along with parameter segments
            return 2;
        }

        var part = segment.Parts[0];
        // Literal segments always go first
        if (part.IsLiteral)
        {
            return 1;
        }
        else
        {
            Debug.Assert(part.IsParameter);
            var digit = part.IsCatchAll ? 5 : 3;

            // If there is a route constraint for the parameter, reduce order by 1
            // Constrained parameters end up with order 2, Constrained catch alls end up with order 4
            if (part.InlineConstraints != null && part.InlineConstraints.Any())
            {
                digit--;
            }

            return digit;
        }
    }
#endif

    // see description on ComputeInboundPrecedenceDigit(TemplateSegment segment)
    //
    // With a RoutePattern, parameters with a required value are treated as a literal segment
    internal static int ComputeInboundPrecedenceDigit(RoutePattern routePattern, RoutePatternPathSegment pathSegment)
    {
        if (pathSegment.Parts.Count > 1)
        {
            // Multi-part segments should appear after literal segments and along with parameter segments
            return 2;
        }

        var part = pathSegment.Parts[0];
        // Literal segments always go first
        if (part.IsLiteral)
        {
            return 1;
        }
        else if (part is RoutePatternParameterPart parameterPart)
        {
            // Parameter with a required value is matched as a literal
            if (routePattern.RequiredValues.TryGetValue(parameterPart.Name, out var requiredValue) &&
                !RouteValueEqualityComparer.Default.Equals(requiredValue, string.Empty))
            {
                return 1;
            }

            var digit = parameterPart.IsCatchAll ? 5 : 3;

            // If there is a route constraint for the parameter, reduce order by 1
            // Constrained parameters end up with order 2, Constrained catch alls end up with order 4
            if (parameterPart.ParameterPolicies.Count > 0)
            {
                digit--;
            }

            return digit;
        }
        else
        {
            // Unreachable
            throw new NotSupportedException();
        }
    }
}
