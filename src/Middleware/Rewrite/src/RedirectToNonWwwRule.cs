// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Rewrite.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Rewrite
{
    internal class RedirectToNonWwwRule : IRule
    {
        public readonly int _statusCode;
        public readonly string[] _domains;

        public RedirectToNonWwwRule(int statusCode)
        {
            _statusCode = statusCode;
        }

        public RedirectToNonWwwRule(int statusCode, params string[] domains)
        {
            if (domains == null)
            {
                throw new ArgumentNullException(nameof(domains));
            }

            if (domains.Length < 1)
            {
                throw new ArgumentException(nameof(domains));
            }

            _domains = domains;
            _statusCode = statusCode;
        }

        public virtual void ApplyRule(RewriteContext context)
        {
            var req = context.HttpContext.Request;

            if (req.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = RuleResult.ContinueRules;
                return;
            }

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

            var nonWwwHost = new HostString(req.Host.Value.Substring(4)); // We verified the hostname begins with "www." already.
            var newUrl = UriHelper.BuildAbsolute(req.Scheme, nonWwwHost, req.PathBase, req.Path, req.QueryString);
            var response = context.HttpContext.Response;
            response.StatusCode = _statusCode;
            response.Headers[HeaderNames.Location] = newUrl;
            context.Result = RuleResult.EndResponse;
            context.Logger.RedirectedToNonWww();
        }
    }
}
