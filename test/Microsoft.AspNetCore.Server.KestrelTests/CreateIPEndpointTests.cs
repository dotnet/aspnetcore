// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class CreateIPEndpointTests
    {
        [Theory]
        [InlineData("10.10.10.10", "10.10.10.10")]
        [InlineData("[::1]", "::1")]
        [InlineData("randomhost", "::")] // "::" is IPAddress.IPv6Any
        [InlineData("*", "::")] // "::" is IPAddress.IPv6Any
        public void CorrectIPEndpointsAreCreated(string host, string expectedAddress)
        {
            var endpoint = KestrelServer.CreateIPEndPoint(ServerAddress.FromUrl($"http://{host}:5000/"));
            Assert.NotNull(endpoint);
            Assert.Equal(IPAddress.Parse(expectedAddress), endpoint.Address);
            Assert.Equal(5000, endpoint.Port);
        }
    }
}
