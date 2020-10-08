// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing;

namespace Interop.FunctionalTests
{
    internal static class Utilities
    {
        internal static bool CurrentPlatformSupportsHTTP2OverTls()
        {
            return // "Missing Windows ALPN support: https://en.wikipedia.org/wiki/Application-Layer_Protocol_Negotiation#Support" or missing compatible ciphers (Win8.1)
                new MinimumOSVersionAttribute(OperatingSystems.Windows, WindowsVersions.Win10).IsMet
                // "Missing SslStream ALPN support: https://github.com/dotnet/runtime/issues/27727"
                && new OSSkipConditionAttribute(OperatingSystems.MacOSX).IsMet;
        }
    }
}
