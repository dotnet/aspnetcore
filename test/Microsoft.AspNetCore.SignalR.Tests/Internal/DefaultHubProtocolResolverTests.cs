// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Protocol.Tests
{
    public class DefaultHubProtocolResolverTests
    {
        [Theory]
        [MemberData(nameof(HubProtocols))]
        public void DefaultHubProtocolResolverTestsCanCreateSupportedProtocols(IHubProtocol protocol)
        {
            var mockConnection = new Mock<HubConnectionContext>(new Mock<ConnectionContext>().Object, TimeSpan.FromSeconds(30), NullLoggerFactory.Instance);
            Assert.IsType(
                protocol.GetType(),
                new DefaultHubProtocolResolver(Options.Create(new HubOptions())).GetProtocol(protocol.Name, mockConnection.Object));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("dummy")]
        public void DefaultHubProtocolResolverThrowsForNotSupportedProtocol(string protocolName)
        {
            var mockConnection = new Mock<HubConnectionContext>(new Mock<ConnectionContext>().Object, TimeSpan.FromSeconds(30), NullLoggerFactory.Instance);
            var exception = Assert.Throws<NotSupportedException>(
                () => new DefaultHubProtocolResolver(Options.Create(new HubOptions())).GetProtocol(protocolName, mockConnection.Object));

            Assert.Equal($"The protocol '{protocolName ?? "(null)"}' is not supported.", exception.Message);
        }

        public static IEnumerable<object[]> HubProtocols =>
            new[]
            {
                new object[] { new JsonHubProtocol() },
                new object[] { new MessagePackHubProtocol() },
            };
    }
}
