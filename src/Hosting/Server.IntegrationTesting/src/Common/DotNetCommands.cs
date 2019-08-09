// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    public static class DotNetCommands
    {
        private const string _dotnetFolderName = ".dotnet";

        internal static string DotNetHome { get; } = GetDotNetHome();

        public static string GetDotNetHome()
        {
            var dotnetHome = Environment.GetEnvironmentVariable("DOTNET_HOME");
            var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");

            var result = Path.Combine(Directory.GetCurrentDirectory(), _dotnetFolderName);
            if (!string.IsNullOrEmpty(dotnetHome))
            {
                result = dotnetHome;
            }
            else if (!string.IsNullOrEmpty(dotnetRoot))
            {
                result = dotnetRoot;
            }

            return result;
        }

        public static string GetDotNetInstallDir(RuntimeArchitecture arch)
        {
            var dotnetDir = DotNetHome;
            // dotnet root on helix is in a different location than on local dev
            // Helix is
            // sdk
            //  /x64
            //  /x86
            // Local dev is
            // .dotnet
            //  *x64 installation here*
            //  /x86
            // This checks if dotnet home is either in a x86 subfolder or not.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && arch == RuntimeArchitecture.x86)
            {
                var dotnetx86Path = Path.Combine(dotnetDir, arch.ToString());
                if (Directory.Exists(dotnetx86Path))
                {
                    dotnetDir = dotnetx86Path;
                }
            }

            return dotnetDir;
        }

        public static string GetDotNetExecutable(RuntimeArchitecture arch)
        {
            var dotnetDir = GetDotNetInstallDir(arch);

            var dotnetFile = "dotnet";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                dotnetFile += ".exe";
            }

            return Path.Combine(dotnetDir, dotnetFile);
        }

        public static bool IsRunningX86OnX64(RuntimeArchitecture arch)
        {
            return (RuntimeInformation.OSArchitecture == Architecture.X64 || RuntimeInformation.OSArchitecture == Architecture.Arm64)
                && arch == RuntimeArchitecture.x86;
        }
    }
}
