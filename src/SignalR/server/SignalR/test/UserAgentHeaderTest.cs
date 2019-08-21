// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            var userAgent = Constants.UserAgentHeader;
            Assert.NotNull(userAgent);
            Assert.StartsWith("Microsoft SignalR/", userAgent);

            var majorVersion = typeof(HttpConnection).Assembly.GetName().Version.Major;
            var minorVersion = typeof(HttpConnection).Assembly.GetName().Version.Minor;

            Assert.Contains($"{majorVersion}.{minorVersion}", userAgent);
        }
    }
}
