// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.AspNetCore.Cors.Infrastructure
{
    internal static class CorsPolicyExtensions
    {
        private const string _WildcardSubdomain = "*.";

        public static bool IsOriginAnAllowedSubdomain(this CorsPolicy policy, string origin)
        {
            if (policy.Origins.Contains(origin))
            {
                return true;
            }

            if (Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
            {
                return policy.Origins
                    .Where(o => o.Contains($"://{_WildcardSubdomain}"))
                    .Select(CreateDomainUri)
                    .Any(domain => UriHelpers.IsSubdomainOf(originUri, domain));
            }

            return false;
        }

        private static Uri CreateDomainUri(string origin)
        {
            return new Uri(origin.Replace(_WildcardSubdomain, string.Empty), UriKind.Absolute);
        }
    }
}