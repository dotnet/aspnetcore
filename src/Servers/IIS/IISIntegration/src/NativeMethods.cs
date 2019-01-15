// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal static class NativeMethods
    {
        private const string KERNEL32 = "kernel32.dll";

        [DllImport(KERNEL32, ExactSpelling = true, SetLastError = true)]

        public static extern bool CloseHandle(IntPtr handle);
    }
}
