// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    public class ModRewriteRedirectAction : UrlAction
    {
        public int StatusCode { get; }
        public bool QueryStringAppend { get; }
        public bool QueryStringDelete { get; }
        public bool EscapeBackReferences { get; }

        public ModRewriteRedirectAction(
            int statusCode,
            Pattern pattern,
            bool queryStringAppend,
            bool queryStringDelete,
            bool escapeBackReferences)
        {
            StatusCode = statusCode;
            Url = pattern;
            QueryStringAppend = queryStringAppend;
            QueryStringDelete = queryStringDelete;
            EscapeBackReferences = escapeBackReferences;
        }

        public override RuleResult ApplyAction(RewriteContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            var pattern = Url.Evaluate(context, ruleMatch, condMatch);
            if (EscapeBackReferences)
            {
                // because escapebackreferences will be encapsulated by the pattern, just escape the pattern
                pattern = Uri.EscapeDataString(pattern);
            }
            context.HttpContext.Response.StatusCode = StatusCode;

            // url can either contain the full url or the path and query
            // always add to location header.
            // TODO check for false positives
            var split = pattern.IndexOf('?');
            if (split >= 0 && QueryStringAppend)
            {
                var query = context.HttpContext.Request.QueryString.Add(
                    QueryString.FromUriComponent(
                        pattern.Substring(split)));

                // not using the response.redirect here because status codes may be 301, 302, 307, 308 
                context.HttpContext.Response.Headers[HeaderNames.Location] = pattern.Substring(0, split) + query;
            }
            else
            {
                // If the request url has a query string and the target does not, append the query string
                // by default.
                if (QueryStringDelete)
                {
                    context.HttpContext.Response.Headers[HeaderNames.Location] = pattern;
                }
                else
                {
                    context.HttpContext.Response.Headers[HeaderNames.Location] = pattern + context.HttpContext.Request.QueryString;
                }
            }
            return RuleResult.ResponseComplete;
        }
    }
}
