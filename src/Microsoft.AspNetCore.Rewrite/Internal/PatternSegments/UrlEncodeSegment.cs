// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using System.Text.Encodings.Web;

namespace Microsoft.AspNetCore.Rewrite.Internal.PatternSegments
{
    public class UrlEncodeSegment : PatternSegment
    {
        private readonly Pattern _pattern;
        
        public UrlEncodeSegment(Pattern pattern)
        {
            _pattern = pattern;
        }

        public override string Evaluate(RewriteContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            var tempBuilder = context.Builder;
            context.Builder = new StringBuilder(64);
            var pattern = _pattern.Evaluate(context, ruleMatch, condMatch);
            context.Builder = tempBuilder;
            return UrlEncoder.Default.Encode(pattern);
        }
    }
}
