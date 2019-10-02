// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal
{
    internal static class Constants
    {
        public const string UserAgent = "User-Agent";
        public static readonly string UserAgentHeader;

        static Constants()
        {
            var assemblyVersion = typeof(Constants)
                .Assembly
                .GetCustomAttributes<AssemblyInformationalVersionAttribute>()
                .FirstOrDefault();

            Debug.Assert(assemblyVersion != null);

            var majorVersion = typeof(Constants).Assembly.GetName().Version.Major;
            var minorVersion = typeof(Constants).Assembly.GetName().Version.Minor;
            var os = RuntimeInformation.OSDescription;
            var runtime = ".NET";
            var runtimeVersion = RuntimeInformation.FrameworkDescription;

            // assembly version attribute should always be present
            // but in case it isn't then don't include version in user-agent
            if (assemblyVersion != null)
            {
                UserAgentHeader = $"Microsoft SignalR/{majorVersion}.{minorVersion} ({assemblyVersion.InformationalVersion}; {os}; {runtime}; {runtimeVersion})";
            }
            else
            {
                UserAgentHeader = $"Microsoft SignalR/{majorVersion}.{minorVersion} ({os}; {runtime}; {runtimeVersion})";
            }
        }
    }
}
