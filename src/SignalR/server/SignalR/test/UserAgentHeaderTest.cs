// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
        [Theory]
        [MemberData(nameof(UserAgents))]
        public void UserAgentHeaderIsCorrect(Version version, string detailedVersion, string os, string runtime, string runtimeVersion, string expected)
        {
            Assert.Equal(expected, Constants.ConstructUserAgent(version, detailedVersion, os, runtime, runtimeVersion));
        }

        public static IEnumerable<object[]> UserAgents()
        {
            yield return new object[] { new Version(1, 4), "1.4.3-preview9", "Windows NT", ".NET", ".NET 4.8.7", "Microsoft SignalR/1.4 (1.4.3-preview9; Windows NT; .NET; .NET 4.8.7)" };
            yield return new object[] { new Version(3, 1), "3.1.0", "", ".NET", ".NET 4.8.9", "Microsoft SignalR/3.1 (3.1.0; Unknown OS; .NET; .NET 4.8.9)" };
            yield return new object[] { new Version(3, 1), "3.1.0", "", ".NET", "", "Microsoft SignalR/3.1 (3.1.0; Unknown OS; .NET; Unknown Runtime Version)" };
            yield return new object[] { new Version(3, 1), "", "Linux", ".NET", ".NET 4.5.1", "Microsoft SignalR/3.1 (Unknown Version; Linux; .NET; .NET 4.5.1)" };
        }
    }
}
