// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using System.Text.Encodings.Web;

namespace Microsoft.AspNetCore.Rewrite.PatternSegments
{
    internal class UrlEncodeSegment : PatternSegment
    {
        private readonly Pattern _pattern;

        public UrlEncodeSegment(Pattern pattern)
        {
            _pattern = pattern;
        }

        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences)
        {
            var oldBuilder = context.Builder;
            // PERF 
            // Because we need to be able to evaluate multiple nested patterns,
            // we provided a new string builder and evaluate the new pattern,
            // and restore it after evaluation.
            context.Builder = new StringBuilder(64);
            var pattern = _pattern.Evaluate(context, ruleBackReferences, conditionBackReferences);
            context.Builder = oldBuilder;
            return UrlEncoder.Default.Encode(pattern);
        }
    }
}
