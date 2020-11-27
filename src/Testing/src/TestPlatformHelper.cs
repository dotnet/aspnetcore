// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Testing
{
    public static class TestPlatformHelper
    {
        public static bool IsMono =>
            Type.GetType("Mono.Runtime") != null;

        public static bool IsWindows =>
            OperatingSystem.IsWindows();

        public static bool IsLinux =>
            OperatingSystem.IsLinux();

        public static bool IsMac =>
            OperatingSystem.IsMacOS();
    }
}
