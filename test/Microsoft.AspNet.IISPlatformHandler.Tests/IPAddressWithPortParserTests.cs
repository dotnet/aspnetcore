// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using Microsoft.AspNet.IISPlatformHandler;
using Xunit;

namespace Microsoft.AspNet.PipelineHandler.Tests
{
    public class IPAddressWithPortParserTests
    {
        [Theory]
        [InlineData("127.0.0.1", "127.0.0.1", null)]
        [InlineData("127.0.0.1:1", "127.0.0.1", 1)]
        [InlineData("1", "0.0.0.1", null)]
        [InlineData("1:1", "0.0.0.1", 1)]
        [InlineData("::1", "::1", null)]
        [InlineData("[::1]", "::1", null)]
        [InlineData("[::1]:1", "::1", 1)]
        public void ParsesCorrectly(string input, string expectedAddress, int? expectedPort)
        {
            IPAddress address;
            int? port;
            var success = IPAddressWithPortParser.TryParse(input, out address, out port);
            Assert.True(success);
            Assert.Equal(expectedAddress, address?.ToString());
            Assert.Equal(expectedPort, port);
        }

        [InlineData("[::1]:")]
        [InlineData("[::1:")]
        [InlineData("::1:")]
        [InlineData("127:")]
        [InlineData("127.0.0.1:")]
        [InlineData("")]
        [InlineData("[]")]
        [InlineData("]")]
        [InlineData("]:1")]
        public void ShouldNotParse(string input)
        {
            IPAddress address;
            int? port;
            var success = IPAddressWithPortParser.TryParse(input, out address, out port);
            Assert.False(success);
            Assert.Equal(null, address);
            Assert.Equal(null, port);
        }
    }
}
