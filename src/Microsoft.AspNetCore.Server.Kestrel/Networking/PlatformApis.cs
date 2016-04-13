// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Networking
{
    public static class PlatformApis
    {
        static PlatformApis()
        {
#if NETSTANDARD1_3
            IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            IsDarwin = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
#else
            var p = (int)Environment.OSVersion.Platform;
            IsWindows = (p != 4) && (p != 6) && (p != 128);

            if (!IsWindows)
            {
                // When running on Mono in Darwin OSVersion doesn't return Darwin. It returns Unix instead.
                IsDarwin = PlatformServices.Default.Runtime.OperatingSystemPlatform == Platform.Darwin;    
            }
#endif
        }

        public static bool IsWindows { get; }

        public static bool IsDarwin { get; }
    }
}
