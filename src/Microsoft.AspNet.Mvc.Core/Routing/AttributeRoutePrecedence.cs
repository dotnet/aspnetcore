// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using Microsoft.AspNet.Routing.Template;

namespace Microsoft.AspNet.Mvc.Routing
{
    /// <summary>
    /// Computes precedence for an attribute route template.
    /// </summary>
    public static class AttributeRoutePrecedence
    {
        public static decimal Compute(Template template)
        {
            // Each precedence digit corresponds to one decimal place. For example, 3 segments with precedences 2, 1,
            // and 4 results in a combined precedence of 2.14 (decimal).
            var precedence = 0m;

            for (var i = 0; i < template.Segments.Count; i++)
            {
                var segment = template.Segments[i];

                var digit = ComputeDigit(segment);
                Contract.Assert(digit >= 0 && digit < 10);

                precedence += Decimal.Divide(digit, (decimal)Math.Pow(10, i));
            }

            return precedence;
        }

        // Segments have the following order:
        // 1 - Literal segments
        // 2 - Constrained parameter segments / Multi-part segments
        // 3 - Unconstrained parameter segments
        // 4 - Constrained wildcard parameter segments
        // 5 - Unconstrained wildcard parameter segments
        private static int ComputeDigit(TemplateSegment segment)
        {
            if (segment.Parts.Count > 1)
            {
                // Multi-part segments should appear after literal segments but before parameter segments
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
                if (part.InlineConstraint != null)
                {
                    digit--;
                }

                return digit;
            }
        }
    }
}