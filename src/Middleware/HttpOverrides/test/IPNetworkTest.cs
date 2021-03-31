// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Xunit;

namespace Microsoft.AspNetCore.HttpOverrides
{
    public class IPNetworkTest
    {
        [Theory]
        [InlineData("10.1.1.0", 8, "10.1.1.10")]
        [InlineData("174.0.0.0", 7, "175.1.1.10")]
        [InlineData("10.174.0.0", 15, "10.175.1.10")]
        [InlineData("10.168.0.0", 14, "10.171.1.10")]
        public void Contains_Positive(string prefixText, int length, string addressText)
        {
            var network = new IPNetwork(IPAddress.Parse(prefixText), length);
            Assert.True(network.Contains(IPAddress.Parse(addressText)));
        }

        [Theory]
        [InlineData("10.1.0.0", 16, "10.2.1.10")]
        [InlineData("174.0.0.0", 7, "173.1.1.10")]
        [InlineData("10.174.0.0", 15, "10.173.1.10")]
        [InlineData("10.168.0.0", 14, "10.172.1.10")]
        public void Contains_Negative(string prefixText, int length, string addressText)
        {
            var network = new IPNetwork(IPAddress.Parse(prefixText), length);
            Assert.False(network.Contains(IPAddress.Parse(addressText)));
        }
    }
}
