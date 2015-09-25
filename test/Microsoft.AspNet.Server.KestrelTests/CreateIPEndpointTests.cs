// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using Microsoft.AspNet.Server.Kestrel.Networking;
using Xunit;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class CreateIPEndpointTests
    {
        [Theory]
        [InlineData("localhost", "127.0.0.1")]
        [InlineData("10.10.10.10", "10.10.10.10")]
        [InlineData("randomhost", "0.0.0.0")]
        public void CorrectIPEndpointsAreCreated(string host, string expectedAddress)
        {
            // "0.0.0.0" is IPAddress.Any
            var endpoint = UvTcpHandle.CreateIPEndpoint(host, 5000);
            Assert.NotNull(endpoint);
            Assert.Equal(IPAddress.Parse(expectedAddress), endpoint.Address);
            Assert.Equal(5000, endpoint.Port);
        }
    }
}
