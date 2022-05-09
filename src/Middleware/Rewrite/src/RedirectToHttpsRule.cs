// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Rewrite.Logging;

namespace Microsoft.AspNetCore.Rewrite;

internal sealed class RedirectToHttpsRule : IRule
{
    public int? SSLPort { get; set; }
    public int StatusCode { get; set; }

    public void ApplyRule(RewriteContext context)
    {
        if (!context.HttpContext.Request.IsHttps)
        {
            var host = context.HttpContext.Request.Host;
            int port;
            if (SSLPort.HasValue && (port = SSLPort.GetValueOrDefault()) > 0)
            {
                // a specific SSL port is specified
                host = new HostString(host.Host, port);
            }
            else
            {
                // clear the port
                host = new HostString(host.Host);
            }

            var req = context.HttpContext.Request;
            var newUrl = UriHelper.BuildAbsolute("https", host, req.PathBase, req.Path, req.QueryString, default);
            var response = context.HttpContext.Response;
            response.StatusCode = StatusCode;
            response.Headers.Location = newUrl;
            context.Result = RuleResult.EndResponse;
            context.Logger.RedirectedToHttps();
        }
    }
}
