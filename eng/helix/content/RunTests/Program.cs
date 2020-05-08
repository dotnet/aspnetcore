// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace RunTests
{
    class Program
    {
        private static class NativeMethods
        {
            [DllImport("Dbghelp.dll")]
            public static extern bool MiniDumpWriteDump(IntPtr hProcess, uint ProcessId, SafeFileHandle hFile, MINIDUMP_TYPE DumpType, IntPtr ExceptionParam, IntPtr UserStreamParam, IntPtr CallbackParam);

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
        static async Task Main(string[] args)
        {
            try
            {
                var runner = new TestRunner(RunTestsOptions.Parse(args));

                var keepGoing = runner.SetupEnvironment();
                if (keepGoing)
                {
                    keepGoing = await runner.InstallAspNetAppIfNeededAsync();
                }
                if (keepGoing)
                {
                    keepGoing = runner.InstallAspNetRefIfNeeded();
                }

                runner.DisplayContents();

                if (keepGoing)
                {

                    _ = Task.Run(async () =>
                    {
                       Console.WriteLine("Waiting 22 minutes");
                       await Task.Delay(10000);
                       Console.WriteLine("Done waiting");
                       try
                       {
                           var dumpDirectoryPath = Environment.GetEnvironmentVariable("HELIX_DUMP_FOLDER");
                           Console.WriteLine($"Dump directory is {dumpDirectoryPath}");
                           var process = Process.GetCurrentProcess();
                           foreach (var dotnetProc in Process.GetProcessesByName("dotnet"))
                           {
                               Console.WriteLine($"Capturing dump of {dotnetProc.Id}");
                               if (dotnetProc.Id == process.Id)
                                   continue;

                               var dumpFilePath = Path.Combine(dumpDirectoryPath, $"{dotnetProc.ProcessName}-{dotnetProc.Id}.dmp");
                               using (var stream = new FileStream(dumpFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                               {
                                   // Dump the process!
                                   //var exceptionInfo = new NativeMethods.MINIDUMP_EXCEPTION_INFORMATION();
                                   if (!NativeMethods.MiniDumpWriteDump(dotnetProc.Handle, (uint)dotnetProc.Id, stream.SafeFileHandle, NativeMethods.MINIDUMP_TYPE.MiniDumpWithFullMemory, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero))
                                   {
                                       var err = Marshal.GetHRForLastWin32Error();
                                       Marshal.ThrowExceptionForHR(err);
                                   }
                               }
                               //await ProcessUtil.RunAsync($"{Environment.GetEnvironmentVariable("HELIX_CORRELATION_PAYLOAD")}/tools/dotnet-dump",
                               //    $"collect -p {dotnetProc.Id} -o \"{dumpFilePath}\"",
                               //    environmentVariables: runner.EnvironmentVariables,
                               //    outputDataReceived: Console.WriteLine,
                               //    errorDataReceived: Console.Error.WriteLine);
                           }
                       }
                       catch (Exception ex)
                       {
                           Console.WriteLine($"Exception getting dump(s) {ex}");
                       }
                    });

                    if (!await runner.CheckTestDiscoveryAsync())
                    {
                        Console.WriteLine("RunTest stopping due to test discovery failure.");
                        Environment.Exit(1);
                        return;
                    }

                    var exitCode = await runner.RunTestsAsync();
                    runner.UploadResults();
                    Console.WriteLine($"Completed Helix job with exit code '{exitCode}'");
                    Environment.Exit(exitCode);
                }

                Console.WriteLine("Tests were not run due to previous failures. Exit code=1");
                Environment.Exit(1);
            }
            catch (Exception e)
            {
                Console.WriteLine($"RunTests uncaught exception: {e.ToString()}");
                Environment.Exit(1);
            }
        }
    }
}
