// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.Internal
{
    public class RedirectToHttpsRule : IRule
    {
        public int? SSLPort { get; set; }
        public int StatusCode { get; set; }

        public virtual void ApplyRule(RewriteContext context)
        {
            if (!context.HttpContext.Request.IsHttps)
            {
                var host = context.HttpContext.Request.Host;
                if (SSLPort.HasValue && SSLPort.Value > 0)
                {
                    // a specific SSL port is specified
                    host = new HostString(host.Host, SSLPort.Value);
                }
                else
                {
                    // clear the port
                    host = new HostString(host.Host);
                }

                var req = context.HttpContext.Request;

                var newUrl = new StringBuilder().Append("https://").Append(host).Append(req.PathBase).Append(req.Path).Append(req.QueryString);
                context.HttpContext.Response.Redirect(newUrl.ToString());
                context.Result = RuleResult.EndResponse;
            }
        }
    }
}