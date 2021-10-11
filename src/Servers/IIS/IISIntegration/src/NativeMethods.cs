// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
