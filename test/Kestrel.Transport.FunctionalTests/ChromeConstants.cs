// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Interop.FunctionalTests
{
    public static class ChromeConstants
    {
        public static string ExecutablePath { get; } = ResolveChromeExecutablePath();

        private static string ResolveChromeExecutablePath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Path.Combine("/usr", "bin", "google-chrome");
            }

            throw new PlatformNotSupportedException();
        }
    }
}
