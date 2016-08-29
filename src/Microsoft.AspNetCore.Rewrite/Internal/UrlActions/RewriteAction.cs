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
        public bool QueryStringAppend { get; }
        public bool QueryStringDelete { get; }
        public bool EscapeBackReferences { get; }

        public RewriteAction(
            RuleTermination result,
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

        public RewriteAction(
            RuleTermination result,
            Pattern pattern,
            bool queryStringAppend):
            this (result, 
                pattern,
                queryStringAppend,
                queryStringDelete: false,
                escapeBackReferences: false)
        {
            
        }

        public override void ApplyAction(RewriteContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            var pattern = Url.Evaluate(context, ruleMatch, condMatch);
            var request = context.HttpContext.Request;

            if (EscapeBackReferences)
            {
                // because escapebackreferences will be encapsulated by the pattern, just escape the pattern
                pattern = Uri.EscapeDataString(pattern);
            }

            // TODO PERF, substrings, object creation, etc.
            if (pattern.IndexOf("://", StringComparison.Ordinal) >= 0)
            {
                string scheme;
                HostString host;
                PathString path;
                QueryString query;
                FragmentString fragment;
                UriHelper.FromAbsolute(pattern, out scheme, out host, out path, out query, out fragment);

                if (query.HasValue)
                {
                    if (QueryStringAppend)
                    {
                        request.QueryString = request.QueryString.Add(query);
                    }
                    else
                    {
                        request.QueryString = query;
                    }
                }
                else if (QueryStringDelete)
                {
                    request.QueryString = QueryString.Empty;
                }

                request.Scheme = scheme;
                request.Host = host;
                request.Path = path;
            }
            else
            {
                var split = pattern.IndexOf('?');
                if (split >= 0)
                {
                    var path = pattern.Substring(0, split);
                    if (path.StartsWith(ForwardSlash))
                    {
                        request.Path = PathString.FromUriComponent(path);
                    }
                    else
                    {
                        request.Path = PathString.FromUriComponent(ForwardSlash + path);
                    }

                    if (QueryStringAppend)
                    {
                        request.QueryString = request.QueryString.Add(
                            QueryString.FromUriComponent(
                                pattern.Substring(split)));
                    }
                    else
                    {
                        request.QueryString = QueryString.FromUriComponent(
                            pattern.Substring(split));
                    }
                }
                else
                {
                    if (pattern.StartsWith(ForwardSlash))
                    {
                        request.Path = PathString.FromUriComponent(pattern);
                    }
                    else
                    {
                        request.Path = PathString.FromUriComponent(ForwardSlash + pattern);
                    }

                    if (QueryStringDelete)
                    {
                        request.QueryString = QueryString.Empty;
                    }
                }
            }
            context.Result = Result;
        }
    }
}
