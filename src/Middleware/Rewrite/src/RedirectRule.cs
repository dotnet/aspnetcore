// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Rewrite.Logging;
using Microsoft.Net.Http.Headers;

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
                throw new ArgumentNullException(nameof(regex));
            }

            if (string.IsNullOrEmpty(replacement))
            {
                throw new ArgumentNullException(nameof(replacement));
            }

            InitialMatch = new Regex(regex, RegexOptions.Compiled | RegexOptions.CultureInvariant, _regexTimeout);
            Replacement = replacement;
            StatusCode = statusCode;
        }

        public virtual void ApplyRule(RewriteContext context)
        {
            var request = context.HttpContext.Request;
            var path = request.Path;
            var pathBase = request.PathBase;

            Match initMatchResults;
            if (!path.HasValue)
            {
                initMatchResults = InitialMatch.Match(string.Empty);
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

                string encodedPath;

                if (string.IsNullOrEmpty(newPath))
                {
                    encodedPath = pathBase.HasValue ? pathBase.Value : "/";
                }
                else
                {
                    var host = default(HostString);
                    var schemeSplit = newPath.IndexOf(Uri.SchemeDelimiter, StringComparison.Ordinal);
                    if (schemeSplit >= 0)
                    {
                        schemeSplit += Uri.SchemeDelimiter.Length;
                        var pathSplit = newPath.IndexOf('/', schemeSplit);

                        if (pathSplit == -1)
                        {
                            host = new HostString(newPath.Substring(schemeSplit));
                            newPath = "/";
                        }
                        else
                        {
                            host = new HostString(newPath.Substring(schemeSplit, pathSplit - schemeSplit));
                            newPath = newPath.Substring(pathSplit);
                        }
                    }

                    if (newPath[0] != '/')
                    {
                        newPath = '/' + newPath;
                    }

                    var resolvedQuery = request.QueryString;
                    var resolvedPath = newPath;
                    var querySplit = newPath.IndexOf('?');
                    if (querySplit >= 0)
                    {
                        resolvedQuery = request.QueryString.Add(QueryString.FromUriComponent(newPath.Substring(querySplit)));
                        resolvedPath = newPath.Substring(0, querySplit);
                    }

                    encodedPath = host.HasValue
                        ? UriHelper.BuildAbsolute(request.Scheme, host, pathBase, resolvedPath, resolvedQuery, default)
                        : UriHelper.BuildRelative(pathBase, resolvedPath, resolvedQuery, default);
                }

                // not using the HttpContext.Response.redirect here because status codes may be 301, 302, 307, 308
                response.Headers.Location = encodedPath;

                context.Logger.RedirectedRequest(newPath);
            }
        }
    }
}
