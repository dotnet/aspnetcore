// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Microsoft.AspNetCore.Rewrite;

internal static class RedirectToWwwHelper
{
    private const string Localhost = "localhost";

    public static bool IsHostInDomains(HttpRequest request, string[]? domains)
    {
        if (request.Host.Host.Equals(Localhost, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (domains != null)
        {
            var isHostInDomains = false;

            foreach (var domain in domains)
            {
                if (domain.Equals(request.Host.Host, StringComparison.OrdinalIgnoreCase))
                {
                    isHostInDomains = true;
                    break;
                }
            }

            if (!isHostInDomains)
            {
                return false;
            }
        }

        return true;
    }

    public static void SetRedirect(RewriteContext context, HostString newHost, int statusCode)
    {
        var request = context.HttpContext.Request;
        var response = context.HttpContext.Response;

        var newUrl = UriHelper.BuildAbsolute(
            request.Scheme,
            newHost,
            request.PathBase,
            request.Path,
            request.QueryString);

        response.StatusCode = statusCode;
        response.Headers.Location = newUrl;
        context.Result = RuleResult.EndResponse;
    }
}
