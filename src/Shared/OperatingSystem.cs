// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETCOREAPP
#error Use System.OperatingSystem instead.
#else

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore
{
    internal sealed class OperatingSystem
    {
        private static readonly bool _isBrowser = RuntimeInformation.IsOSPlatform(OSPlatform.Create("browser"));

        public static bool IsBrowser() => _isBrowser;
    }
}
#endif
