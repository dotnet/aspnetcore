// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNet.Routing.Template;

namespace Microsoft.AspNet.Mvc.Routing
{
    /// <summary>
    /// Computes precedence for an attribute route template.
    /// </summary>
    public static class AttributeRoutePrecedence
    {
        // Compute the precedence for matching a provided url
        // e.g.: /api/template == 1.1
        //       /api/template/{id} == 1.13
        //       /api/{id:int} == 1.2
        //       /api/template/{id:int} == 1.12
        public static decimal ComputeMatched(RouteTemplate template)
        {
            // Each precedence digit corresponds to one decimal place. For example, 3 segments with precedences 2, 1,
            // and 4 results in a combined precedence of 2.14 (decimal).
            var precedence = 0m;

            for (var i = 0; i < template.Segments.Count; i++)
            {
                var segment = template.Segments[i];

                var digit = ComputeMatchDigit(segment);
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
        public static decimal ComputeGenerated(RouteTemplate template)
        {
            // Each precedence digit corresponds to one decimal place. For example, 3 segments with precedences 2, 1,
            // and 4 results in a combined precedence of 2.14 (decimal).
            var precedence = 0m;

            for (var i = 0; i < template.Segments.Count; i++)
            {
                var segment = template.Segments[i];

                var digit = ComputeGenerationDigit(segment);
                Debug.Assert(digit >= 0 && digit < 10);

                precedence += decimal.Divide(digit, (decimal)Math.Pow(10, i));
            }

            return precedence;
        }

        // Segments have the following order:
        // 5 - Literal segments
        // 4 - Multi-part segments && Constrained parameter segments
        // 3 - Unconstrained parameter segements
        // 2 - Constrained wildcard parameter segments
        // 1 - Unconstrained wildcard parameter segments
        private static int ComputeGenerationDigit(TemplateSegment segment)
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

        // Segments have the following order:
        // 1 - Literal segments
        // 2 - Constrained parameter segments / Multi-part segments
        // 3 - Unconstrained parameter segments
        // 4 - Constrained wildcard parameter segments
        // 5 - Unconstrained wildcard parameter segments
        private static int ComputeMatchDigit(TemplateSegment segment)
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
    }
}