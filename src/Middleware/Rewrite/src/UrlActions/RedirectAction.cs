// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Rewrite.UrlActions
{
    internal class RedirectAction : UrlAction
    {
        public int StatusCode { get; }
        public bool QueryStringAppend { get; }
        public bool QueryStringDelete { get; }
        public bool EscapeBackReferences { get; }

        public RedirectAction(
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

        public RedirectAction(
            int statusCode,
            Pattern pattern,
            bool queryStringAppend)
            : this(
                statusCode,
                pattern,
                queryStringAppend,
                queryStringDelete: true,
                escapeBackReferences: false)
        {
        }

        public override void ApplyAction(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences)
        {
            var pattern = Url.Evaluate(context, ruleBackReferences, conditionBackReferences);
            var response = context.HttpContext.Response;
            var pathBase = context.HttpContext.Request.PathBase;
            if (EscapeBackReferences)
            {
                // because escapebackreferences will be encapsulated by the pattern, just escape the pattern
                pattern = Uri.EscapeDataString(pattern);
            }

            if (string.IsNullOrEmpty(pattern))
            {
                response.Headers[HeaderNames.Location] = pathBase.HasValue ? pathBase.Value : "/";
                return;
            }


            if (pattern.IndexOf("://", StringComparison.Ordinal) == -1 && pattern[0] != '/')
            {
                pattern = '/' + pattern;
            }
            response.StatusCode = StatusCode;

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
                response.Headers[HeaderNames.Location] = pathBase + pattern.Substring(0, split) + query;
            }
            else
            {
                // If the request url has a query string and the target does not, append the query string
                // by default.
                if (QueryStringDelete)
                {
                    response.Headers[HeaderNames.Location] = pathBase + pattern;
                }
                else
                {
                    response.Headers[HeaderNames.Location] = pathBase + pattern + context.HttpContext.Request.QueryString;
                }
            }
            context.Result = RuleResult.EndResponse;
        }
    }
}
