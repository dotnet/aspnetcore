// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Constants = Microsoft.AspNetCore.Http.Connections.Client.Internal.Constants;

namespace Microsoft.AspNetCore.Http.Connections.Tests
{
    public class UserAgentHeaderTest
    {
        [Theory]
        [MemberData(nameof(UserAgentTestDataNames))]
        public void UserAgentHeaderIsCorrect(string testDataName)
        {
            var testData = UserAgents[testDataName];
            Assert.Equal(testData.Expected, Constants.ConstructUserAgent(testData.Version, testData.DetailedVersion, testData.Os, testData.Runtime, testData.RuntimeVersion));
        }

        public static Dictionary<string, UserAgentTestData> UserAgents => new[]
        {
            new UserAgentTestData("FullInfo", new Version(1, 4), "1.4.3-preview9", "Windows NT", ".NET", ".NET 4.8.7", "Microsoft SignalR/1.4 (1.4.3-preview9; Windows NT; .NET; .NET 4.8.7)"),
            new UserAgentTestData("EmptyOs", new Version(3, 1), "3.1.0", "", ".NET", ".NET 4.8.9", "Microsoft SignalR/3.1 (3.1.0; Unknown OS; .NET; .NET 4.8.9)"),
            new UserAgentTestData("EmptyRuntimeVersion", new Version(3, 1), "3.1.0", "", ".NET", "", "Microsoft SignalR/3.1 (3.1.0; Unknown OS; .NET; Unknown Runtime Version)"),
            new UserAgentTestData("EmptyDetailedVersion", new Version(3, 1), "", "Linux", ".NET", ".NET 4.5.1", "Microsoft SignalR/3.1 (Unknown Version; Linux; .NET; .NET 4.5.1)"),
        }.ToDictionary(t => t.Name);

        public static IEnumerable<object[]> UserAgentTestDataNames => UserAgents.Keys.Select(name => new object[] { name });

        public class UserAgentTestData
        {
            public string Name { get; }
            public Version Version { get; }
            public string DetailedVersion { get; }
            public string Os { get; }
            public string Runtime { get; }
            public string RuntimeVersion { get; }
            public string Expected { get; }

            public UserAgentTestData(string name, Version version, string detailedVersion, string os, string runtime, string runtimeVersion, string expected)
            {
                Name = name;
                Version = version;
                DetailedVersion = detailedVersion;
                Os = os;
                Runtime = runtime;
                RuntimeVersion = runtimeVersion;
                Expected = expected;
            }
        }
    }
}
