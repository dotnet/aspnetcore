// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http.Connections.Client;
using Xunit;
using Constants = Microsoft.AspNetCore.Http.Connections.Client.Internal.Constants;

namespace Microsoft.AspNetCore.Http.Connections.Tests
{
    public class UserAgentHeaderTest
    {
        [Fact]
        public void UserAgentHeaderIsAccurate()
        {
            var majorVersion = typeof(HttpConnection).Assembly.GetName().Version.Major;
            var minorVersion = typeof(HttpConnection).Assembly.GetName().Version.Minor;
            var version = typeof(HttpConnection).Assembly.GetName().Version;
            var os = RuntimeInformation.OSDescription;
            var runtime = ".NET";
            var runtimeVersion = RuntimeInformation.FrameworkDescription;
            var assemblyVersion = typeof(Constants)
                .Assembly
                .GetCustomAttributes<AssemblyInformationalVersionAttribute>()
                .FirstOrDefault();
            var userAgent = Constants.UserAgentHeader;
            var expectedUserAgent = $"Microsoft SignalR/{majorVersion}.{minorVersion} ({assemblyVersion.InformationalVersion}; {os}; {runtime}; {runtimeVersion})";

            Assert.Equal(expectedUserAgent, userAgent);
        }
    }
}
