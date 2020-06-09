// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing;

namespace Interop.FunctionalTests
{
    internal static class Utilities
    {
        internal static bool CurrentPlatformSupportsAlpn()
        {
            return // "Missing Windows ALPN support: https://en.wikipedia.org/wiki/Application-Layer_Protocol_Negotiation#Support"
                new MinimumOSVersionAttribute(OperatingSystems.Windows, WindowsVersions.Win81).IsMet
                // "Missing SslStream ALPN support: https://github.com/dotnet/corefx/issues/30492"
                && new OSSkipConditionAttribute(OperatingSystems.MacOSX).IsMet
                // Debian 8 uses OpenSSL 1.0.1 which does not support ALPN
                && new SkipOnHelixAttribute("https://github.com/dotnet/aspnetcore/issues/10428") { Queues = "Debian.8.Amd64;Debian.8.Amd64.Open" }.IsMet;
        }
    }
}
