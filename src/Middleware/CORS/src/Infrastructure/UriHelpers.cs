// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Cors.Infrastructure;

internal static class UriHelpers
{
    public static bool IsSubdomainOf(Uri subdomain, Uri domain)
    {
        return subdomain.IsAbsoluteUri
            && domain.IsAbsoluteUri
            && subdomain.Scheme == domain.Scheme
            && subdomain.Port == domain.Port
            && subdomain.Host.EndsWith($".{domain.Host}", StringComparison.Ordinal);
    }
}
