// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Extensions.Logging.Testing
{
    public static partial class DumpCollector
    {
        private static class Windows
        {
            internal static void Collect(Process process, string outputFile)
            {
                // Open the file for writing
                using (var stream = new FileStream(outputFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                {
                    // Dump the process!
                    var exceptionInfo = new NativeMethods.MINIDUMP_EXCEPTION_INFORMATION();
                    if (!NativeMethods.MiniDumpWriteDump(process.Handle, (uint)process.Id, stream.SafeFileHandle, NativeMethods.MINIDUMP_TYPE.MiniDumpWithFullMemory, ref exceptionInfo, IntPtr.Zero, IntPtr.Zero))
                    {
                        var err = Marshal.GetHRForLastWin32Error();
                        Marshal.ThrowExceptionForHR(err);
                    }
                }
            }

            private static class NativeMethods
            {
                [DllImport("Dbghelp.dll")]
                public static extern bool MiniDumpWriteDump(IntPtr hProcess, uint ProcessId, SafeFileHandle hFile, MINIDUMP_TYPE DumpType, ref MINIDUMP_EXCEPTION_INFORMATION ExceptionParam, IntPtr UserStreamParam, IntPtr CallbackParam);

                [StructLayout(LayoutKind.Sequential, Pack = 4)]
                public struct MINIDUMP_EXCEPTION_INFORMATION
                {
                    public uint ThreadId;
                    public IntPtr ExceptionPointers;
                    public int ClientPointers;
                }

                [Flags]
                public enum MINIDUMP_TYPE : uint
                {
                    MiniDumpNormal = 0,
                    MiniDumpWithDataSegs = 1 << 0,
                    MiniDumpWithFullMemory = 1 << 1,
                    MiniDumpWithHandleData = 1 << 2,
                    MiniDumpFilterMemory = 1 << 3,
                    MiniDumpScanMemory = 1 << 4,
                    MiniDumpWithUnloadedModules = 1 << 5,
                    MiniDumpWithIndirectlyReferencedMemory = 1 << 6,
                    MiniDumpFilterModulePaths = 1 << 7,
                    MiniDumpWithProcessThreadData = 1 << 8,
                    MiniDumpWithPrivateReadWriteMemory = 1 << 9,
                    MiniDumpWithoutOptionalData = 1 << 10,
                    MiniDumpWithFullMemoryInfo = 1 << 11,
                    MiniDumpWithThreadInfo = 1 << 12,
                    MiniDumpWithCodeSegs = 1 << 13,
                    MiniDumpWithoutAuxiliaryState = 1 << 14,
                    MiniDumpWithFullAuxiliaryState = 1 << 15,
                    MiniDumpWithPrivateWriteCopyMemory = 1 << 16,
                    MiniDumpIgnoreInaccessibleMemory = 1 << 17,
                    MiniDumpWithTokenInformation = 1 << 18,
                    MiniDumpWithModuleHeaders = 1 << 19,
                    MiniDumpFilterTriage = 1 << 20,
                    MiniDumpWithAvxXStateContext = 1 << 21,
                    MiniDumpWithIptTrace = 1 << 22,
                    MiniDumpValidTypeFlags = (-1) ^ ((~1) << 22)
                }
            }
        }
    }
}
