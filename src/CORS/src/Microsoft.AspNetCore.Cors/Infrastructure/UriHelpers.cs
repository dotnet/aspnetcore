// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Cors.Infrastructure
{
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
}