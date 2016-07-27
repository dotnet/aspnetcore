// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite.PatternSegments
{
    public class UrlSegment : PatternSegment
    {
        public override string Evaluate(HttpContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            return context.Request.Path;
        }
    }
}
