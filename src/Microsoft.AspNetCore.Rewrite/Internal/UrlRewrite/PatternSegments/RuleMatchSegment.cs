// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite.PatternSegments
{
    public class RuleMatchSegment : PatternSegment
    {
        public int Index { get; set; }

        public RuleMatchSegment(int index)
        {
            Index = index;
        }

        public override string Evaluate(HttpContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            return ruleMatch?.BackReference[Index]?.Value;
        }
    }
}
