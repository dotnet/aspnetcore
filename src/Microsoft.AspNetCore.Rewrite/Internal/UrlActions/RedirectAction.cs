// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlActions
{
    public class RedirectAction : UrlAction
    {
        public int StatusCode { get; }
        public bool AppendQueryString { get; }

        public RedirectAction(int statusCode, Pattern pattern, bool appendQueryString)
        {
            StatusCode = statusCode;
            Url = pattern;
            AppendQueryString = appendQueryString;
        }

        public override void ApplyAction(RewriteContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            var pattern = Url.Evaluate(context, ruleMatch, condMatch);
            context.HttpContext.Response.StatusCode = StatusCode;

            // TODO IIS guarantees that there will be a leading slash
            if (pattern.IndexOf("://", StringComparison.Ordinal) == -1 && !pattern.StartsWith("/"))
            {
                pattern = '/' + pattern;
            }

            // url can either contain the full url or the path and query
            // always add to location header.
            // TODO check for false positives
            var split = pattern.IndexOf('?');
            if (split >= 0 && AppendQueryString)
            {
                var query = context.HttpContext.Request.QueryString.Add(
                    QueryString.FromUriComponent(
                        pattern.Substring(split)));
                // not using the HttpContext.Response.redirect here because status codes may be 301, 302, 307, 308 
                context.HttpContext.Response.Headers[HeaderNames.Location] = pattern.Substring(0, split) + query;
            }
            else
            {
                context.HttpContext.Response.Headers[HeaderNames.Location] = pattern;
            }
            context.Result = RuleTermination.ResponseComplete;
        }
    }
}
