// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using System.Text.Encodings.Web;

namespace Microsoft.AspNetCore.Rewrite.Internal.PatternSegments
{
    public class UrlEncodeSegment : PatternSegment
    {
        public Pattern Pattern { get; set; }
        
        public UrlEncodeSegment(Pattern pattern)
        {
            Pattern = pattern;
        }

        public override string Evaluate(RewriteContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            var tempBuilder = context.Builder;
            context.Builder = new StringBuilder(64);
            var pattern = Pattern.Evaluate(context, ruleMatch, condMatch);
            context.Builder = tempBuilder;
            return UrlEncoder.Default.Encode(pattern);
        }
    }
}
