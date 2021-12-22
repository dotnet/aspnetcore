// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETCOREAPP
#error Use System.OperatingSystem instead.
#else

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore;

internal sealed class OperatingSystem
{
#if NETFRAMEWORK
    private const bool _isBrowser = false;
#else
    private static readonly bool _isBrowser = RuntimeInformation.IsOSPlatform(OSPlatform.Create("browser"));
#endif

    public static bool IsBrowser() => _isBrowser;
}
#endif
