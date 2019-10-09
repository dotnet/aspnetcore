// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.HttpSys.Internal
{
    public class HttpSysGetHttpProtocolVersionTest
    {
        public static TheoryData<Version, string> s_data = new TheoryData<Version, string>
        {
            { new Version(2, 0), "HTTP/2" },
            { new Version(1, 1), "HTTP/1.1" },
            { new Version(1, 0), "HTTP/1.0" },
            { new Version(0, 3), "HTTP/0.3" },
            { new Version(2, 1), "HTTP/2.1" }
        };

        [Theory]
        [MemberData(nameof(s_data))]
        public void GetHttpProtocolVersion_CorrectIETFVersion(Version version, string expected)
        {
            var actual = version.GetHttpProtocolVersion();

            Assert.Equal(expected, actual);
        }
    }
}
