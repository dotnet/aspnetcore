// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite
{
    public class Pattern
    {
        public IList<PatternSegment> PatternSegments { get; }
        public Pattern(List<PatternSegment> patternSegments)
        {
            PatternSegments = patternSegments;
        }

        public string Evaluate(HttpContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            var strBuilder = new StringBuilder();

            // TODO consider thread static for string builder - DAVID PERF
            foreach (var pattern in PatternSegments)
            {
                strBuilder.Append(pattern.Evaluate(context, ruleMatch, condMatch));
            }
            return strBuilder.ToString();
        }
    }
}
