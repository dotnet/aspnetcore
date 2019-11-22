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
        public void RegisteringMultipleHubProtocolsReplacesWithLatest()
        {
            var jsonProtocol1 = new NewtonsoftJsonHubProtocol();
            var jsonProtocol2 = new NewtonsoftJsonHubProtocol();
            var resolver = new DefaultHubProtocolResolver(new[] {
                jsonProtocol1,
                jsonProtocol2
            }, NullLogger<DefaultHubProtocolResolver>.Instance);

            var resolvedProtocol = resolver.GetProtocol(jsonProtocol2.Name, null);
            Assert.NotSame(jsonProtocol1, resolvedProtocol);
            Assert.Same(jsonProtocol2, resolvedProtocol);
        }

        [Fact]
        public void AllProtocolsOnlyReturnsLatestOfSameType()
        {
            var jsonProtocol1 = new NewtonsoftJsonHubProtocol();
            var jsonProtocol2 = new NewtonsoftJsonHubProtocol();
            var resolver = new DefaultHubProtocolResolver(new[] {
                jsonProtocol1,
                jsonProtocol2
            }, NullLogger<DefaultHubProtocolResolver>.Instance);

            var hubProtocols = resolver.AllProtocols;
            Assert.Equal(1, hubProtocols.Count);

            Assert.Same(jsonProtocol2, hubProtocols[0]);
        }

        public static IEnumerable<object[]> HubProtocolNames => HubProtocolHelpers.AllProtocols.Select(p => new object[] {p.Name});
    }
}
