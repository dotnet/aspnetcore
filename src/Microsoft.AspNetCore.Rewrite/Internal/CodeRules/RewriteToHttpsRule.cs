// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.Internal.CodeRules
{
    public class RewriteToHttpsRule : Rule
    {

        public bool stopProcessing { get; set; }
        public int? SSLPort { get; set; }
        public override RuleResult ApplyRule(RewriteContext context)
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

                context.HttpContext.Request.Scheme = "https";
                context.HttpContext.Request.Host = host;
                return stopProcessing ? RuleResult.StopRules: RuleResult.Continue;
            }
            return RuleResult.Continue;
        }
    }
}
