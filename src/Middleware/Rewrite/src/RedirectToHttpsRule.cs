// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Rewrite.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Rewrite
{
    internal class RedirectToHttpsRule : IRule
    {
        public int? SSLPort { get; set; }
        public int StatusCode { get; set; }

        public virtual void ApplyRule(RewriteContext context)
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
                response.Headers[HeaderNames.Location] = newUrl;
                context.Result = RuleResult.EndResponse;
                context.Logger.RedirectedToHttps();
            }
        }
    }
}
