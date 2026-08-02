// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure;

/// <summary>
/// This route pattern comparer checks to see if two route patterns match the same URL and create ambiguous match exceptions at runtime.
/// It doesn't check two routes exactly equal each other. For example, "/product/{id}" and "/product/{name}" aren't exactly equal but will match the same URL.
/// </summary>
internal sealed class AmbiguousRoutePatternComparer : IEqualityComparer<RoutePatternTree>
{
    public static AmbiguousRoutePatternComparer Instance { get; } = new();

    public bool Equals(RoutePatternTree x, RoutePatternTree y)
    {
        if (x.Root.Parts.Length != y.Root.Parts.Length)
        {
            return false;
        }

        for (var i = 0; i < x.Root.Parts.Length; i++)
        {
            var xPart = x.Root.Parts[i];
            var yPart = y.Root.Parts[i];

            var equal = xPart switch
            {
                RoutePatternSegmentSeparatorNode _ => yPart is RoutePatternSegmentSeparatorNode,
                RoutePatternSegmentNode xSegment => Equals(xSegment, yPart as RoutePatternSegmentNode),
                _ => throw new InvalidOperationException($"Unexpected part type '{xPart.Kind}'."),
            };

            if (!equal)
            {
                return false;
            }
        }

        return true;
    }

    private static bool Equals(RoutePatternSegmentNode x, RoutePatternSegmentNode? y)
    {
        if (y is null)
        {
            return false;
        }

        if (x.Children.Length != y.Children.Length)
        {
            return false;
        }

        for (var i = 0; i < x.Children.Length; i++)
        {
            var xChild = x.Children[i];
            var yChild = y.Children[i];

            var equal = xChild switch
            {
                RoutePatternOptionalSeparatorNode _ => yChild is RoutePatternOptionalSeparatorNode,
                RoutePatternReplacementNode xReplacement => yChild is RoutePatternReplacementNode yReplacement && IgnoreCaseEquals(xReplacement.TextToken.Value, yReplacement.TextToken.Value),
                RoutePatternLiteralNode xLiteral => yChild is RoutePatternLiteralNode yLiteral && IgnoreCaseEquals(xLiteral.LiteralToken.Value, yLiteral.LiteralToken.Value),
                RoutePatternParameterNode xParameter => Equals(xParameter, yChild as RoutePatternParameterNode),
                _ => throw new InvalidOperationException($"Unexpected segment node type '{xChild.Kind}'."),
            };

            if (!equal)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IgnoreCaseEquals(object? value1, object? value2)
    {
        var s1 = value1 as string;
        var s2 = value2 as string;

        if (s1 is null || s2 is null)
        {
            return false;
        }

        return string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);
    }

    private static bool Equals(RoutePatternParameterNode x, RoutePatternParameterNode? y)
    {
        if (y is null)
        {
            return false;
        }

        // Only parameter policies differentiate between parameters.
        var xParameterPolicies = x.ParameterParts.Where(p => p.Kind == RoutePatternKind.ParameterPolicy).OfType<RoutePatternPolicyParameterPartNode>().ToList();
        var yParameterPolicies = y.ParameterParts.Where(p => p.Kind == RoutePatternKind.ParameterPolicy).OfType<RoutePatternPolicyParameterPartNode>().ToList();

        if (xParameterPolicies.Count != yParameterPolicies.Count)
        {
            return false;
        }

        for (var i = 0; i < xParameterPolicies.Count; i++)
        {
            var xPolicy = xParameterPolicies[i];
            var yPolicy = yParameterPolicies[i];

            if (!Equals(xPolicy, yPolicy))
            {
                return false;
            }
        }

        return true;
    }

    private static bool Equals(RoutePatternPolicyParameterPartNode x, RoutePatternPolicyParameterPartNode y)
    {
        if (x.PolicyFragments.Length != y.PolicyFragments.Length)
        {
            return false;
        }

        for (var i = 0; i < x.PolicyFragments.Length; i++)
        {
            var xPart = x.PolicyFragments[i];
            var yPart = y.PolicyFragments[i];

            var equal = xPart switch
            {
                RoutePatternPolicyFragment xFragment => yPart is RoutePatternPolicyFragment yFragment && Equals(xFragment.ArgumentToken.Value, yFragment.ArgumentToken.Value),
                RoutePatternPolicyFragmentEscapedNode xFragmentEscaped => yPart is RoutePatternPolicyFragmentEscapedNode yFragmentEscaped && Equals(xFragmentEscaped.ArgumentToken.Value, yFragmentEscaped.ArgumentToken.Value),
                _ => throw new InvalidOperationException($"Unexpected policy node type '{xPart.Kind}'."),
            };

            if (!equal)
            {
                return false;
            }
        }

        return true;
    }

    public int GetHashCode(RoutePatternTree obj)
    {
        // TODO: Improve hash code calculation. This is rudimentary and will generate a lot of collisions.
        return obj.Root.ChildCount;
    }
}
