// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;

namespace Microsoft.AspNetCore.Rewrite.Internal.PatternSegments
{
    public class ToLowerSegment : PatternSegment
    {
        public Pattern Pattern { get; set; }

        public ToLowerSegment(Pattern pattern)
        {
            Pattern = pattern;
        }

        public override string Evaluate(RewriteContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            // PERF as we share the string builder across the context, we need to make a new one here to evaluate
            // lowercase segments.
            var tempBuilder = context.Builder;
            context.Builder = new StringBuilder(64);
            var pattern = Pattern.Evaluate(context, ruleMatch, condMatch);
            context.Builder = tempBuilder;
            return pattern.ToLowerInvariant();
        }
    }
}
