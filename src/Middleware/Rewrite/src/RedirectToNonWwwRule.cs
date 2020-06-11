// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.Logging;

namespace Microsoft.AspNetCore.Rewrite
{
    internal class RedirectToNonWwwRule : IRule
    {
        private const string WwwDot = "www.";

        private readonly string[] _domains;
        private readonly int _statusCode;

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
                throw new ArgumentException($"One or more {nameof(domains)} must be provided.");
            }

            _domains = domains;
            _statusCode = statusCode;
        }

        public void ApplyRule(RewriteContext context)
        {
            var request = context.HttpContext.Request;

            var hostInDomains = RedirectToWwwHelper.IsHostInDomains(request, _domains);

            if (!hostInDomains)
            {
                context.Result = RuleResult.ContinueRules;
                return;
            }

            if (!request.Host.Value.StartsWith(WwwDot, StringComparison.OrdinalIgnoreCase))
            {
                context.Result = RuleResult.ContinueRules;
                return;
            }

            RedirectToWwwHelper.SetRedirect(
                context,
                new HostString(request.Host.Value.Substring(4)), // We verified the hostname begins with "www." already.
                _statusCode);

            context.Logger.RedirectedToNonWww();
        }
    }
}
