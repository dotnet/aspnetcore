// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlActions
{
    public class RewriteAction : UrlAction
    {
        private readonly string ForwardSlash = "/";
        public RuleTermination Result { get; }
        public bool ClearQuery { get; }

        public RewriteAction(RuleTermination result, Pattern pattern, bool clearQuery)
        {
            Result = result;
            Url = pattern;
            ClearQuery = clearQuery;
        }

        public override void ApplyAction(RewriteContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            var pattern = Url.Evaluate(context, ruleMatch, condMatch);

            if (ClearQuery)
            {
                context.HttpContext.Request.QueryString = QueryString.Empty;
            }

            if (pattern.IndexOf("://", StringComparison.Ordinal) >= 0)
            {
                string scheme;
                HostString host;
                PathString path;
                QueryString query;
                FragmentString fragment;
                UriHelper.FromAbsolute(pattern, out scheme, out host, out path, out query, out fragment);

                context.HttpContext.Request.Scheme = scheme;
                context.HttpContext.Request.Host = host;
                context.HttpContext.Request.Path = path;
                context.HttpContext.Request.QueryString = query.Add(context.HttpContext.Request.QueryString);
            }
            else
            {
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
                    context.HttpContext.Request.QueryString = context.HttpContext.Request.QueryString.Add(
                        QueryString.FromUriComponent(
                            pattern.Substring(split)));
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
            context.Result = Result;
        }
    }
}
