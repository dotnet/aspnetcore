// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

internal sealed class RoutePatternComparer : IEqualityComparer<RoutePatternTree>
{
    public static RoutePatternComparer Instance { get; } = new();

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
                RoutePatternSegmentSeperatorNode _ => yPart is RoutePatternSegmentSeperatorNode,
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
            var xPart = x.Children[i];
            var yPart = y.Children[i];

            var equal = xPart switch
            {
                RoutePatternOptionalSeperatorNode _ => yPart is RoutePatternOptionalSeperatorNode,
                RoutePatternReplacementNode xReplacement => yPart is RoutePatternReplacementNode yReplacement && Equals(xReplacement.TextToken.Value, yReplacement.TextToken.Value),
                RoutePatternLiteralNode xLiteral => yPart is RoutePatternLiteralNode yLiteral && Equals(xLiteral.LiteralToken.Value, yLiteral.LiteralToken.Value),
                RoutePatternParameterNode xParameter => Equals(xParameter, yPart as RoutePatternParameterNode),
                _ => throw new InvalidOperationException($"Unexpected segment node type '{xPart.Kind}'."),
            };

            if (!equal)
            {
                return false;
            }
        }

        return true;
    }

    private static bool Equals(RoutePatternParameterNode x, RoutePatternParameterNode? y)
    {
        if (y is null)
        {
            return false;
        }

        // Only parameter policies can be used to differentiate between parameters.
        var xParameterPolicies = x.ParameterParts.Where(p => p.Kind == RoutePatternKind.ParameterPolicy).OfType<RoutePatternPolicyParameterPartNode>().ToList();
        var yParameterPolicies = y.ParameterParts.Where(p => p.Kind == RoutePatternKind.ParameterPolicy).OfType<RoutePatternPolicyParameterPartNode>().ToList();

        if (xParameterPolicies.Count != yParameterPolicies.Count)
        {
            return false;
        }

        for (var i = 0; i < xParameterPolicies.Count; i++)
        {
            var xPart = xParameterPolicies[i];
            var yPart = yParameterPolicies[i];

            if (!Equals(xPart, yPart))
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
                _ => throw new InvalidOperationException($"Unexpected segment node type '{xPart.Kind}'."),
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
        return obj.Root.ChildCount;
    }
}
