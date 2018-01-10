// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Protocol.Tests
{
    public class DefaultHubProtocolResolverTests
    {
        private static readonly IList<IHubProtocol> AllProtocols = new List<IHubProtocol>()
        {
            new JsonHubProtocol(),
            new MessagePackHubProtocol()
        };

        [Theory]
        [MemberData(nameof(HubProtocols))]
        public void DefaultHubProtocolResolverTestsCanCreateSupportedProtocols(IHubProtocol protocol)
        {
            var mockConnection = new Mock<HubConnectionContext>(new Mock<ConnectionContext>().Object, TimeSpan.FromSeconds(30), NullLoggerFactory.Instance);
            var resolver = new DefaultHubProtocolResolver(Options.Create(new HubOptions()), AllProtocols, NullLogger<DefaultHubProtocolResolver>.Instance);
            Assert.IsType(
                protocol.GetType(),
                resolver.GetProtocol(protocol.Name, mockConnection.Object));
        }

        [Fact]
        public void DefaultHubProtocolResolverThrowsForNullProtocol()
        {
            var mockConnection = new Mock<HubConnectionContext>(new Mock<ConnectionContext>().Object, TimeSpan.FromSeconds(30), NullLoggerFactory.Instance);
            var resolver = new DefaultHubProtocolResolver(Options.Create(new HubOptions()), AllProtocols, NullLogger<DefaultHubProtocolResolver>.Instance);
            var exception = Assert.Throws<ArgumentNullException>(
                () => resolver.GetProtocol(null, mockConnection.Object));

            Assert.Equal("protocolName", exception.ParamName);
        }

        [Fact]
        public void DefaultHubProtocolResolverThrowsForNotSupportedProtocol()
        {
            var mockConnection = new Mock<HubConnectionContext>(new Mock<ConnectionContext>().Object, TimeSpan.FromSeconds(30), NullLoggerFactory.Instance);
            var resolver = new DefaultHubProtocolResolver(Options.Create(new HubOptions()), AllProtocols, NullLogger<DefaultHubProtocolResolver>.Instance);
            var exception = Assert.Throws<NotSupportedException>(
                () => resolver.GetProtocol("notARealProtocol", mockConnection.Object));

            Assert.Equal("The protocol 'notARealProtocol' is not supported.", exception.Message);
        }

        [Fact]
        public void RegisteringMultipleHubProtocolsFails()
        {
            var mockConnection = new Mock<HubConnectionContext>(new Mock<ConnectionContext>().Object, TimeSpan.FromSeconds(30), NullLoggerFactory.Instance);
            var exception = Assert.Throws<InvalidOperationException>(() => new DefaultHubProtocolResolver(Options.Create(new HubOptions()), new[] {
                new JsonHubProtocol(),
                new JsonHubProtocol()
            }, NullLogger<DefaultHubProtocolResolver>.Instance));

            Assert.Equal($"Multiple Hub Protocols with the name '{JsonHubProtocol.ProtocolName}' were registered.", exception.Message);
        }

        public static IEnumerable<object[]> HubProtocols =>
            new[]
            {
                new object[] { new JsonHubProtocol() },
                new object[] { new MessagePackHubProtocol() },
            };
    }
}
