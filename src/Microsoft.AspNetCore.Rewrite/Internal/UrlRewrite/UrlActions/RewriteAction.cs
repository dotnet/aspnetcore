// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite.UrlActions
{
    public class RewriteAction : UrlAction
    {
        public RuleTerminiation Result { get; }
        public bool ClearQuery { get; }

        public RewriteAction(RuleTerminiation result, Pattern pattern, bool clearQuery)
        {
            Result = result;
            Url = pattern;
            ClearQuery = clearQuery;
        }

        public override RuleResult ApplyAction(HttpContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            var pattern = Url.Evaluate(context, ruleMatch, condMatch);

            if (ClearQuery)
            {
                context.Request.QueryString = new QueryString();
            }
            // TODO PERF, substrings, object creation, etc.
            if (pattern.IndexOf("://") >= 0)
            {
                string scheme = null;
                var host = new HostString();
                var path = new PathString();
                var query = new QueryString();
                var fragment = new FragmentString();
                UriHelper.FromAbsolute(pattern, out scheme, out host, out path, out query, out fragment);

                context.Request.Scheme = scheme;
                context.Request.Host = host;
                context.Request.Path = path;
                context.Request.QueryString = query.Add(context.Request.QueryString);
            }
            else
            {
                var split = pattern.IndexOf('?');
                if (split >= 0)
                {
                    context.Request.Path = new PathString("/" + pattern.Substring(0, split));
                    context.Request.QueryString = context.Request.QueryString.Add(new QueryString(pattern.Substring(split)));
                }
                else
                {
                    context.Request.Path = new PathString("/" + pattern);
                }
            }
            return new RuleResult { Result = Result };
        }
    }
}
