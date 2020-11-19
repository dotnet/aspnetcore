// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Internal
{
    internal static class ProcessExtensions
    {
        private static readonly bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

        public static void KillTree(this Process process) => process.KillTree(_defaultTimeout);

        public static void KillTree(this Process process, TimeSpan timeout)
        {
            var pid = process.Id;
            if (_isWindows)
            {
                RunProcessAndWaitForExit(
                    "taskkill",
                    $"/T /F /PID {pid}",
                    timeout,
                    out var _);
            }
            else
            {
                process.Kill(entireProcessTree: true);
            }
        }

        private static void RunProcessAndWaitForExit(string fileName, string arguments, TimeSpan timeout, out string stdout)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            var process = Process.Start(startInfo);

            stdout = null;
            if (process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                stdout = process.StandardOutput.ReadToEnd();
            }
            else
            {
                process.Kill();
            }
        }
    }
}
