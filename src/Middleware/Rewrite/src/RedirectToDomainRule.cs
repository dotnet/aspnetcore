// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Rewrite.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Rewrite
{
    internal class RedirectToDomainRule : IRule
    {
        public readonly int _statusCode;
        public readonly string[] _domains;

        public RedirectToDomainRule(int statusCode)
        {
            _statusCode = statusCode;
        }

        public RedirectToDomainRule(int statusCode, params string[] domains)
        {
            if (domains == null)
            {
                throw new ArgumentNullException(nameof(domains));
            }

            if (domains.Length < 1)
            {
                throw new ArgumentException(nameof(domains));
            }

            foreach(var domain in domains)
            {
                if (!domain.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
                {
                    throw new NotSupportedException($"Domain: {domain}. Not supported for this redirection rule. Domain should start with www.");
                }
            }

            _domains = domains;
            _statusCode = statusCode;
        }

        public virtual void ApplyRule(RewriteContext context)
        {
            var req = context.HttpContext.Request;

            if (!req.Host.Value.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = RuleResult.ContinueRules;
                return;
            }

            if (_domains != null)
            {
                var isHostInDomains = false;

                foreach (var domain in _domains)
                {
                    if (domain.Equals(req.Host.Host, StringComparison.OrdinalIgnoreCase))
                    {
                        isHostInDomains = true;
                        break;
                    }
                }

                if (!isHostInDomains)
                {
                    context.Result = RuleResult.ContinueRules;
                    return;
                }
            }

            var wwwHost = new HostString(req.Host.Value.Remove(0, 4));
            var newUrl = UriHelper.BuildAbsolute(req.Scheme, wwwHost, req.PathBase, req.Path, req.QueryString);
            var response = context.HttpContext.Response;
            response.StatusCode = _statusCode;
            response.Headers[HeaderNames.Location] = newUrl;
            context.Result = RuleResult.EndResponse;
            context.Logger.RedirectedToWww();
        }
    }
}
