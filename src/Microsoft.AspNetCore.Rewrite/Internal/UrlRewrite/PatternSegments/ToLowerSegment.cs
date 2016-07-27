// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite.PatternSegments
{
    public class ToLowerSegment : PatternSegment
    {
        public Pattern Pattern { get; set; }

        public ToLowerSegment(Pattern pattern)
        {
            Pattern = pattern;
        }

        public override string Evaluate(HttpContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            var pattern = Pattern.Evaluate(context, ruleMatch, condMatch);
            return pattern.ToLowerInvariant();
        }
    }
}
