// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Dispatcher.Patterns;

namespace Microsoft.AspNetCore.Dispatcher
{
    /// <summary>
    /// Computes precedence for a route pattern.
    /// </summary>
    public static class RoutePrecedence
    {
        // Compute the precedence for matching a provided url
        // e.g.: /api/template == 1.1
        //       /api/template/{id} == 1.13
        //       /api/{id:int} == 1.2
        //       /api/template/{id:int} == 1.12
        public static decimal ComputeInbound(RoutePattern routePattern)
        {
            // Each precedence digit corresponds to one decimal place. For example, 3 segments with precedences 2, 1,
            // and 4 results in a combined precedence of 2.14 (decimal).
            var precedence = 0m;

            for (var i = 0; i < routePattern.PathSegments.Count; i++)
            {
                var segment = routePattern.PathSegments[i];

                var digit = ComputeInboundPrecedenceDigit(segment);
                Debug.Assert(digit >= 0 && digit < 10);

                precedence += decimal.Divide(digit, (decimal)Math.Pow(10, i));
            }

            return precedence;
        }

        // Segments have the following order:
        // 1 - Literal segments
        // 2 - Constrained parameter segments / Multi-part segments
        // 3 - Unconstrained parameter segments
        // 4 - Constrained wildcard parameter segments
        // 5 - Unconstrained wildcard parameter segments
        private static int ComputeInboundPrecedenceDigit(RoutePatternPathSegment segment)
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
                var parameter = (RoutePatternParameter)part;
                var digit = parameter.IsCatchAll ? 5 : 3;

                // If there is a dispatcher value constraint for the parameter, reduce order by 1
                // Constrained parameters end up with order 2, Constrained catch alls end up with order 4
                if (parameter.Constraints != null && parameter.Constraints.Any())
                {
                    digit--;
                }

                return digit;
            }
        }
    }
}