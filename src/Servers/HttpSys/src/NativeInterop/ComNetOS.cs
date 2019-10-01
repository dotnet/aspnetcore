// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal static class ComNetOS
    {
        // Windows is assumed based on HttpApi.Supported which is checked in the HttpSysListener constructor.
        // Minimum support for Windows 7 is assumed.
        internal static readonly bool IsWin8orLater;

        static ComNetOS()
        {
            var win8Version = new Version(6, 2);

            IsWin8orLater = (Environment.OSVersion.Version >= win8Version);
        }
    }
}
