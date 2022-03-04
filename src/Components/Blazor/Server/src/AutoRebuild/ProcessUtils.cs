// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Blazor.Server.AutoRebuild
{
    internal static class ProcessUtils
    {
        // Based on https://stackoverflow.com/a/3346055

        public static Process GetParent(Process process)
        {
            var result = new ProcessBasicInformation();
            var handle = process.Handle;
            var status = NtQueryInformationProcess(handle, 0, ref result, Marshal.SizeOf(result), out var returnLength);
            if (status != 0)
            {
                throw new Win32Exception(status);
            }

            try
            {
                var parentProcessId = result.InheritedFromUniqueProcessId.ToInt32();
                return parentProcessId > 0 ? Process.GetProcessById(parentProcessId) : null;
            }
            catch (ArgumentException)
            {
                return null; // Process not found
            }
        }

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ProcessBasicInformation processInformation, int processInformationLength, out int returnLength);

        [StructLayout(LayoutKind.Sequential)]
        struct ProcessBasicInformation
        {
            // These members must match PROCESS_BASIC_INFORMATION
            public IntPtr Reserved1;
            public IntPtr PebBaseAddress;
            public IntPtr Reserved2_0;
            public IntPtr Reserved2_1;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
        }
    }
}
