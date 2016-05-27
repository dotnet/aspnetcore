// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Networking
{
    public static class PlatformApis
    {
        static PlatformApis()
        {
            IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            IsDarwin = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        }

        public static bool IsWindows { get; }

        public static bool IsDarwin { get; }
    }
}
