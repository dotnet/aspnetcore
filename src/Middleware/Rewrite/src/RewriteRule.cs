// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Rewrite.Logging;

namespace Microsoft.AspNetCore.Rewrite;

internal sealed class RewriteRule : IRule
{
    private readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(1);
    public Regex InitialMatch { get; }
    public string Replacement { get; }
    public bool StopProcessing { get; }
    public RewriteRule(string regex, string replacement, bool stopProcessing)
    {
        ArgumentException.ThrowIfNullOrEmpty(regex);
        ArgumentException.ThrowIfNullOrEmpty(replacement);

        InitialMatch = new Regex(regex, RegexOptions.Compiled | RegexOptions.CultureInvariant, _regexTimeout);
        Replacement = replacement;
        StopProcessing = stopProcessing;
    }

    public void ApplyRule(RewriteContext context)
    {
        var path = context.HttpContext.Request.Path;
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
            var result = initMatchResults.Result(Replacement);
            var request = context.HttpContext.Request;

            if (StopProcessing)
            {
                context.Result = RuleResult.SkipRemainingRules;
            }

            if (string.IsNullOrEmpty(result))
            {
                result = "/";
            }

            if (result.Contains(Uri.SchemeDelimiter, StringComparison.Ordinal))
            {
                string scheme;
                HostString host;
                PathString pathString;
                QueryString query;
                UriHelper.FromAbsolute(result, out scheme, out host, out pathString, out query, out _);

                request.Scheme = scheme;
                request.Host = host;
                request.Path = pathString;
                request.QueryString = query.Add(request.QueryString);
            }
            else
            {
                var split = result.IndexOf('?');
                if (split >= 0)
                {
                    var newPath = result.Substring(0, split);
                    if (newPath[0] == '/')
                    {
                        request.Path = PathString.FromUriComponent(newPath);
                    }
                    else
                    {
                        request.Path = PathString.FromUriComponent('/' + newPath);
                    }
                    request.QueryString = request.QueryString.Add(
                        QueryString.FromUriComponent(
                            result.Substring(split)));
                }
                else
                {
                    if (result[0] == '/')
                    {
                        request.Path = PathString.FromUriComponent(result);
                    }
                    else
                    {
                        request.Path = PathString.FromUriComponent('/' + result);
                    }
                }
            }

            context.Logger.RewrittenRequest(result);
        }
    }
}
