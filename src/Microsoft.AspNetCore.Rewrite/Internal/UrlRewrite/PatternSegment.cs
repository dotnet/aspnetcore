// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite
{
    public abstract class PatternSegment
    {
        //                                                 Match from prevRule, Match from prevCond
        public abstract string Evaluate(HttpContext context, MatchResults ruleMatch, MatchResults condMatch);
    }
}
