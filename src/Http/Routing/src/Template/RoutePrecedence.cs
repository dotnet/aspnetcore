// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Template
{
    /// <summary>
    /// Computes precedence for a route template.
    /// </summary>
    public static class RoutePrecedence
    {
        // Compute the precedence for matching a provided url
        // e.g.: /api/template == 1.1
        //       /api/template/{id} == 1.13
        //       /api/{id:int} == 1.2
        //       /api/template/{id:int} == 1.12
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

        // Compute the precedence for generating a url
        // e.g.: /api/template          == 5.5
        //       /api/template/{id}     == 5.53
        //       /api/{id:int}          == 5.4
        //       /api/template/{id:int} == 5.54
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

        // Segments have the following order:
        // 5 - Literal segments
        // 4 - Multi-part segments && Constrained parameter segments
        // 3 - Unconstrained parameter segements
        // 2 - Constrained wildcard parameter segments
        // 1 - Unconstrained wildcard parameter segments
        private static int ComputeOutboundPrecedenceDigit(TemplateSegment segment)
        {
            if(segment.Parts.Count > 1)
            {
                return 4;
            }

            var part = segment.Parts[0];
            if(part.IsLiteral)
            {
                return 5;
            }
            else
            {
                Debug.Assert(part.IsParameter);
                var digit = part.IsCatchAll ? 1 :  3;

                if (part.InlineConstraints != null && part.InlineConstraints.Any())
                {
                    digit++;
                }

                return digit;
            }
        }

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

        // see description on ComputeInboundPrecedenceDigit(TemplateSegment segment)
        //
        // With a RoutePattern, parameters with a required value are treated as a literal segment
        private static int ComputeInboundPrecedenceDigit(RoutePattern routePattern, RoutePatternPathSegment pathSegment)
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
}