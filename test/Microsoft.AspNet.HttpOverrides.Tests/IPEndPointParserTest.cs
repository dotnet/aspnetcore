// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using Xunit;

namespace Microsoft.AspNet.HttpOverrides
{
    public class IPEndPointParserTests
    {
        [Theory]
        [InlineData("127.0.0.1", "127.0.0.1", 0)]
        [InlineData("127.0.0.1:1", "127.0.0.1", 1)]
        [InlineData("1", "0.0.0.1", 0)]
        [InlineData("1:1", "0.0.0.1", 1)]
        [InlineData("::1", "::1", 0)]
        [InlineData("[::1]", "::1", 0)]
        [InlineData("[::1]:1", "::1", 1)]
        public void ParsesCorrectly(string input, string expectedAddress, int expectedPort)
        {
            IPEndPoint endpoint;
            var success = IPEndPointParser.TryParse(input, out endpoint);
            Assert.True(success);
            Assert.Equal(expectedAddress, endpoint.Address.ToString());
            Assert.Equal(expectedPort, endpoint.Port);
        }

        [InlineData(null)]
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
            IPEndPoint endpoint;
            var success = IPEndPointParser.TryParse(input, out endpoint);
            Assert.False(success);
            Assert.Equal(null, endpoint);
        }
    }
}