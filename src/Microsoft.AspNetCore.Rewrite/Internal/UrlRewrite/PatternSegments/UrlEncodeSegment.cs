// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite.PatternSegments
{
    public class UrlEncodeSegment : PatternSegment
    {
        public Pattern Pattern { get; set; }
        
        public UrlEncodeSegment(Pattern pattern)
        {
            Pattern = pattern;
        }

        public override string Evaluate(HttpContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            var pattern = Pattern.Evaluate(context, ruleMatch, condMatch);
            return UrlEncoder.Default.Encode(pattern);
        }
    }
}
