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

        // Compare to https://github.com/aspnet/BuildTools/blob/314c98e4533217a841ff9767bb38e144eb6c93e4/tools/KoreBuild.Console/Commands/CommandContext.cs#L76
        public static string GetDotNetHome()
        {
            // runtest.* scripts throughout the repo define $env:DOTNET_HOME
            var dotnetHome = Environment.GetEnvironmentVariable("DOTNET_HOME");
            // /activate.* and runtest.* scripts define $env:DOTNET_ROOT and (for /activate.*) $env:{DOTNET_ROOT(x86)}
            var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            // /eng/common/tools.* scripts define $env:DOTNET_INSTALL_DIR
            var dotnetInstallDir = Environment.GetEnvironmentVariable("DOTNET_INSTALL_DIR");

            var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
            var home = Environment.GetEnvironmentVariable("HOME");

            var result = Path.Combine(Directory.GetCurrentDirectory(), _dotnetFolderName);
            if (!string.IsNullOrEmpty(dotnetHome))
            {
                result = dotnetHome;
            }
            else if (!string.IsNullOrEmpty(dotnetRoot))
            {
                if (dotnetRoot.EndsWith("x64"))
                {
                    // DOTNET_ROOT has x64 appended to the path, which we append again in GetDotNetInstallDir
                    result = dotnetRoot[0..^3];
                }
                else
                {
                    result = dotnetRoot;
                }
            }
            else if (!string.IsNullOrEmpty(dotnetInstallDir))
            {
                result = dotnetInstallDir;
            }
            else if (!string.IsNullOrEmpty(userProfile))
            {
                result = Path.Combine(userProfile, _dotnetFolderName);
            }
            else if (!string.IsNullOrEmpty(home))
            {
                result = home;
            }

            return result;
        }

        public static string GetDotNetInstallDir(RuntimeArchitecture arch)
        {
            var dotnetDir = DotNetHome;
            var archSpecificDir = Path.Combine(dotnetDir, arch.ToString());
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Directory.Exists(archSpecificDir))
            {
                dotnetDir = archSpecificDir;
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
