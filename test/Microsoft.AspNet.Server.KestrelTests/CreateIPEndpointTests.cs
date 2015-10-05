// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using Microsoft.AspNet.Server.Kestrel;
using Microsoft.AspNet.Server.Kestrel.Networking;
using Xunit;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class CreateIPEndpointTests
    {
        [Theory]
        [InlineData("localhost", "127.0.0.1")] // https://github.com/aspnet/KestrelHttpServer/issues/231
        [InlineData("10.10.10.10", "10.10.10.10")]
        [InlineData("[::1]", "::1")]
        [InlineData("randomhost", "::")] // "::" is IPAddress.IPv6Any
        [InlineData("*", "::")] // "::" is IPAddress.IPv6Any
        public void CorrectIPEndpointsAreCreated(string host, string expectedAddress)
        {
            var endpoint = UvTcpHandle.CreateIPEndpoint(ServerAddress.FromUrl($"http://{host}:5000/"));
            Assert.NotNull(endpoint);
            Assert.Equal(IPAddress.Parse(expectedAddress), endpoint.Address);
            Assert.Equal(5000, endpoint.Port);
        }
    }
}
