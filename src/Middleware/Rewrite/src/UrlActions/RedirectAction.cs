// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.UrlActions;

internal sealed class RedirectAction : UrlAction
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

    public override void ApplyAction(RewriteContext context, BackReferenceCollection? ruleBackReferences, BackReferenceCollection? conditionBackReferences)
    {
        var pattern = Url!.Evaluate(context, ruleBackReferences, conditionBackReferences);
        var response = context.HttpContext.Response;
        var pathBase = context.HttpContext.Request.PathBase;
        if (EscapeBackReferences)
        {
            // because escapebackreferences will be encapsulated by the pattern, just escape the pattern
            pattern = Uri.EscapeDataString(pattern);
        }

        if (string.IsNullOrEmpty(pattern))
        {
            response.Headers.Location = pathBase.HasValue ? pathBase.Value : "/";
            return;
        }

        if (!pattern.Contains(Uri.SchemeDelimiter, StringComparison.Ordinal) && pattern[0] != '/')
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
            response.Headers.Location = pathBase + pattern.Substring(0, split) + query;
        }
        else
        {
            // If the request url has a query string and the target does not, append the query string
            // by default.
            if (QueryStringDelete)
            {
                response.Headers.Location = pathBase + pattern;
            }
            else
            {
                response.Headers.Location = pathBase + pattern + context.HttpContext.Request.QueryString;
            }
        }
        context.Result = RuleResult.EndResponse;
    }
}
