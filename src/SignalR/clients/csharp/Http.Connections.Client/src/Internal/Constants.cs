// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal
{
    internal static class Constants
    {
        public static readonly string UserAgentHeader;

        static Constants()
        {
            // Microsoft SignalR/[Version] ([Detailed Version]; [Operating System]; [Runtime]; [Runtime Version])
            var userAgent = "";

            var assemblyVersion = typeof(Constants)
                .Assembly
                .GetCustomAttributes<AssemblyInformationalVersionAttribute>()
                .FirstOrDefault();

            Debug.Assert(assemblyVersion != null);


            var majorVersion = typeof(Constants).Assembly.GetName().Version.Major;
            var minorVersion = typeof(Constants).Assembly.GetName().Version.Minor;
            var os = getOperatingSystem();
            var runtime = ".NET";
            var runtimeVersion = RuntimeInformation.FrameworkDescription;

            // assembly version attribute should always be present
            // but in case it isn't then don't include version in user-agent
            if (assemblyVersion != null)
            {
                userAgent = $"Microsoft SignalR/{majorVersion}.{minorVersion} ({assemblyVersion.InformationalVersion}; {os}; {runtime}; {runtimeVersion})";
            }

            //UserAgentHeader = new ProductInfoHeaderValue("Microsoft SignalR", assemblyVersion.InformationalVersion);
            UserAgentHeader = userAgent;
        }

        public static string getOperatingSystem()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "Windows NT";
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "macOS";
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "Linux";
            }

            return "";
        }
    }
}
