// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.UrlRewrite
{
    public class Pattern
    {
        public IList<PatternSegment> PatternSegments { get; }

        public Pattern(List<PatternSegment> patternSegments)
        {
            PatternSegments = patternSegments;
        }

        public string Evaluate(HttpContext context, Match ruleMatch, Match condMatch)
        {
            var strBuilder = new StringBuilder();
            foreach (var pattern in PatternSegments)
            {
                strBuilder.Append(pattern.Evaluate(context, ruleMatch, condMatch));
            }
            return strBuilder.ToString();
        }
    }
}
