// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;

namespace Microsoft.AspNetCore.Rewrite.PatternSegments
{
    internal class ToLowerSegment : PatternSegment
    {
        private readonly Pattern _pattern;

        public ToLowerSegment(Pattern pattern)
        {
            _pattern = pattern;
        }

        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences)
        {
            // PERF as we share the string builder across the context, we need to make a new one here to evaluate
            // lowercase segments.
            var tempBuilder = context.Builder;
            context.Builder = new StringBuilder(64);
            var pattern = _pattern.Evaluate(context, ruleBackReferences, conditionBackReferences);
            context.Builder = tempBuilder;
            return pattern.ToLowerInvariant();
        }
    }
}
