// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.HttpSys;

internal static class ComNetOS
{
    // Windows is assumed based on HttpApi.Supported which is checked in the HttpSysListener constructor.
    // Minimum support for Windows 7 is assumed.
    internal static readonly bool IsWin8orLater = OperatingSystem.IsWindowsVersionAtLeast(6, 2);
}
