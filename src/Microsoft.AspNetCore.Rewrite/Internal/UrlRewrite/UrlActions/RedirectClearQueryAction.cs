// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite.UrlActions
{
    public class RedirectClearQueryAction : UrlAction
    {
        public int StatusCode { get; }
        public RedirectClearQueryAction(int statusCode, Pattern pattern)
        {
            StatusCode = statusCode;
            Url = pattern;
        }

        public override RuleResult ApplyAction(HttpContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            var pattern = Url.Evaluate(context, ruleMatch, condMatch);
            context.Response.StatusCode = StatusCode;

            // we are clearing the query, so just put the pattern in the location header
            context.Response.Headers[HeaderNames.Location] = pattern;
            return RuleResult.ResponseComplete;
        }
    }
}
