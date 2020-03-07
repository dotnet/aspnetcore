// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Rewrite.Logging;

namespace Microsoft.AspNetCore.Rewrite
{
    internal class RedirectRule : IRule
    {
        private readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(1);
        public Regex InitialMatch { get; }
        public string Replacement { get; }
        public int StatusCode { get; }
        public RedirectRule(string regex, string replacement, int statusCode)
        {
            if (string.IsNullOrEmpty(regex))
            {
                throw new ArgumentException(nameof(regex));
            }

            if (string.IsNullOrEmpty(replacement))
            {
                throw new ArgumentException(nameof(replacement));
            }

            InitialMatch = new Regex(regex, RegexOptions.Compiled | RegexOptions.CultureInvariant, _regexTimeout);
            Replacement = replacement;
            StatusCode = statusCode;
        }

        public virtual void ApplyRule(RewriteContext context)
        {
            var path = context.HttpContext.Request.Path;
            var pathBase = context.HttpContext.Request.PathBase;

            Match initMatchResults;
            if (path == PathString.Empty)
            {
                initMatchResults = InitialMatch.Match(path.ToString());
            }
            else
            {
                initMatchResults = InitialMatch.Match(path.ToString().Substring(1));
            }


            if (initMatchResults.Success)
            {
                var newPath = initMatchResults.Result(Replacement);
                var response = context.HttpContext.Response;

                response.StatusCode = StatusCode;
                context.Result = RuleResult.EndResponse;

                if (string.IsNullOrEmpty(newPath))
                {
                    response.Headers[HeaderNames.Location] = pathBase.HasValue ? pathBase.Value : "/";
                    return;
                }

                if (newPath.IndexOf("://", StringComparison.Ordinal) == -1 && newPath[0] != '/')
                {
                    newPath = '/' + newPath;
                }

                var split = newPath.IndexOf('?');
                if (split >= 0)
                {
                    var query = context.HttpContext.Request.QueryString.Add(
                        QueryString.FromUriComponent(
                            newPath.Substring(split)));
                    // not using the HttpContext.Response.redirect here because status codes may be 301, 302, 307, 308
                    response.Headers[HeaderNames.Location] = pathBase + newPath.Substring(0, split) + query.ToUriComponent();
                }
                else
                {
                    response.Headers[HeaderNames.Location] = pathBase + newPath + context.HttpContext.Request.QueryString.ToUriComponent();
                }

                context.Logger.RedirectedRequest(newPath);
            }
        }
    }
}
