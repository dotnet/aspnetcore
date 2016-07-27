// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.Internal
{
    public class SchemeRule : Rule
    {
        public int? SSLPort { get; set; }
        public Transformation OnCompletion { get; set; } = Transformation.Rewrite;
        public override RuleResult ApplyRule(RewriteContext context)
        {

            // TODO this only does http to https, add more features in the future. 
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

                if ((OnCompletion != Transformation.Redirect))
                {
                    context.HttpContext.Request.Scheme = "https";
                    context.HttpContext.Request.Host = host;
                    if (OnCompletion == Transformation.TerminatingRewrite)
                    {
                        return RuleResult.StopRules;
                    }
                    else
                    {
                        return RuleResult.Continue;
                    }
                }

                var req = context.HttpContext.Request;

                var newUrl = new StringBuilder().Append("https://").Append(host).Append(req.PathBase).Append(req.Path).Append(req.QueryString);
                context.HttpContext.Response.Redirect(newUrl.ToString());
                return RuleResult.ResponseComplete;
            }
            return RuleResult.Continue;
        }
    }
}
