// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Protocol.Tests
{
    public class DefaultHubProtocolResolverTests
    {
        [Theory]
        [MemberData(nameof(HubProtocolNames))]
        public void DefaultHubProtocolResolverTestsCanCreateAllProtocols(string protocolName)
        {
            var protocol = HubProtocolHelpers.GetHubProtocol(protocolName);

            var resolver = new DefaultHubProtocolResolver(HubProtocolHelpers.AllProtocols, NullLogger<DefaultHubProtocolResolver>.Instance);
            Assert.IsType(
                protocol.GetType(),
                resolver.GetProtocol(protocol.Name, HubProtocolHelpers.AllProtocolNames));
        }

        [Theory]
        [MemberData(nameof(HubProtocolNames))]
        public void DefaultHubProtocolResolverCreatesProtocolswhenSupoortedProtocolsIsNull(string protocolName)
        {
            var protocol = HubProtocolHelpers.GetHubProtocol(protocolName);

            List<string> supportedProtocols = null;
            var resolver = new DefaultHubProtocolResolver(HubProtocolHelpers.AllProtocols, NullLogger<DefaultHubProtocolResolver>.Instance);
            Assert.IsType(
                protocol.GetType(),
                resolver.GetProtocol(protocol.Name, supportedProtocols));
        }

        [Theory]
        [MemberData(nameof(HubProtocolNames))]
        public void DefaultHubProtocolResolverTestsCanCreateSupportedProtocols(string protocolName)
        {
            var protocol = HubProtocolHelpers.GetHubProtocol(protocolName);

            var supportedProtocols = new List<string> { protocol.Name };
            var resolver = new DefaultHubProtocolResolver(HubProtocolHelpers.AllProtocols, NullLogger<DefaultHubProtocolResolver>.Instance);
            Assert.IsType(
                protocol.GetType(),
                resolver.GetProtocol(protocol.Name, supportedProtocols));
        }

        [Fact]
        public void DefaultHubProtocolResolverThrowsForNullProtocol()
        {
            var resolver = new DefaultHubProtocolResolver(HubProtocolHelpers.AllProtocols, NullLogger<DefaultHubProtocolResolver>.Instance);
            var exception = Assert.Throws<ArgumentNullException>(
                () => resolver.GetProtocol(null, HubProtocolHelpers.AllProtocolNames));

            Assert.Equal("protocolName", exception.ParamName);
        }

        [Fact]
        public void DefaultHubProtocolResolverReturnsNullForNotSupportedProtocol()
        {
            var resolver = new DefaultHubProtocolResolver(HubProtocolHelpers.AllProtocols, NullLogger<DefaultHubProtocolResolver>.Instance);
            Assert.Null(resolver.GetProtocol("notARealProtocol", HubProtocolHelpers.AllProtocolNames));
        }

        [Fact]
        public void RegisteringMultipleHubProtocolsFails()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => new DefaultHubProtocolResolver(new[] {
                new NewtonsoftJsonHubProtocol(),
                new NewtonsoftJsonHubProtocol()
            }, NullLogger<DefaultHubProtocolResolver>.Instance));

            Assert.Equal($"Multiple Hub Protocols with the name 'json' were registered.", exception.Message);
        }

        public static IEnumerable<object[]> HubProtocolNames => HubProtocolHelpers.AllProtocols.Select(p => new object[] {p.Name});
    }
}
