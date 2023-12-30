// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Rewrite.Logging;

namespace Microsoft.AspNetCore.Rewrite;

internal sealed class RedirectRule : IRule
{
    private readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(1);
    public Regex InitialMatch { get; }
    public string Replacement { get; }
    public int StatusCode { get; }
    public RedirectRule(string regex, string replacement, int statusCode)
    {
        ArgumentException.ThrowIfNullOrEmpty(regex);
        ArgumentException.ThrowIfNullOrEmpty(replacement);

        InitialMatch = new Regex(regex, RegexOptions.Compiled | RegexOptions.CultureInvariant, _regexTimeout);
        Replacement = replacement;
        StatusCode = statusCode;
    }

    public void ApplyRule(RewriteContext context)
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
                string scheme = request.Scheme;
                if (schemeSplit >= 0)
                {
                    scheme = newPath.Substring(0, schemeSplit);
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
                    ? UriHelper.BuildAbsolute(scheme, host, pathBase, resolvedPath, resolvedQuery, default)
                    : UriHelper.BuildRelative(pathBase, resolvedPath, resolvedQuery, default);
            }

            // not using the HttpContext.Response.redirect here because status codes may be 301, 302, 307, 308
            response.Headers.Location = encodedPath;

            context.Logger.RedirectedRequest(newPath);
        }
    }
}
