// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

namespace Microsoft.AspNetCore.Cors.Infrastructure;

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
