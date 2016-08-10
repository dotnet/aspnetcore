// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Rewrite.Internal;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    public class ModRewriteRewriteAction : UrlAction
    {
        private readonly string ForwardSlash = "/";
        public RuleResult Result { get; }
        public bool QueryStringAppend { get; }
        public bool QueryStringDelete { get; }
        public bool EscapeBackReferences { get; }

        public ModRewriteRewriteAction(
            RuleResult result, 
            Pattern pattern, 
            bool queryStringAppend,
            bool queryStringDelete,
            bool escapeBackReferences)
        {
            Result = result;
            Url = pattern;
            QueryStringAppend = queryStringAppend;
            QueryStringDelete = queryStringDelete;
            EscapeBackReferences = escapeBackReferences;
        }

        public override RuleResult ApplyAction(RewriteContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            var pattern = Url.Evaluate(context, ruleMatch, condMatch);

            // TODO PERF, substrings, object creation, etc.
            if (pattern.IndexOf("://") >= 0)
            {
                string scheme = null;
                HostString host;
                PathString path;
                QueryString query;
                FragmentString fragment;
                UriHelper.FromAbsolute(pattern, out scheme, out host, out path, out query, out fragment);

                if (query.HasValue)
                {
                    if (QueryStringAppend)
                    {
                        context.HttpContext.Request.QueryString = context.HttpContext.Request.QueryString.Add(query);
                    }
                    else
                    {
                        context.HttpContext.Request.QueryString = query;
                    }
                }
                else if (QueryStringDelete)
                {
                    context.HttpContext.Request.QueryString = QueryString.Empty;
                }

                context.HttpContext.Request.Scheme = scheme;
                context.HttpContext.Request.Host = host;
                context.HttpContext.Request.Path = path;
            }
            else
            {
                // TODO fix with redirect action logic
                var split = pattern.IndexOf('?');
                if (split >= 0)
                {
                    var path = pattern.Substring(0, split);
                    if (path.StartsWith(ForwardSlash))
                    {
                        context.HttpContext.Request.Path = PathString.FromUriComponent(path);
                    }
                    else
                    {
                        context.HttpContext.Request.Path = PathString.FromUriComponent(ForwardSlash + path);
                    }
                    context.HttpContext.Request.QueryString = context.HttpContext.Request.QueryString.Add(new QueryString(pattern.Substring(split)));
                }
                else
                {
                    if (pattern.StartsWith(ForwardSlash))
                    {
                        context.HttpContext.Request.Path = PathString.FromUriComponent(pattern);
                    }
                    else
                    {
                        context.HttpContext.Request.Path = PathString.FromUriComponent(ForwardSlash + pattern);
                    }
                }
            }
            return Result;
        }
    }
}
