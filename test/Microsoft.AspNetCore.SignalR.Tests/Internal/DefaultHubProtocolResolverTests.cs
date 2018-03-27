// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Protocol.Tests
{
    public class DefaultHubProtocolResolverTests
    {
        private static readonly List<string> AllProtocolNames = new List<string> { "json", "messagepack" };

        private static readonly IList<IHubProtocol> AllProtocols = new List<IHubProtocol>()
        {
            new JsonHubProtocol(),
            new MessagePackHubProtocol()
        };


        [Theory]
        [MemberData(nameof(HubProtocols))]
        public void DefaultHubProtocolResolverTestsCanCreateAllProtocols(IHubProtocol protocol)
        {
            var resolver = new DefaultHubProtocolResolver(AllProtocols, NullLogger<DefaultHubProtocolResolver>.Instance);
            Assert.IsType(
                protocol.GetType(),
                resolver.GetProtocol(protocol.Name, AllProtocolNames));
        }

        [Theory]
        [MemberData(nameof(HubProtocols))]
        public void DefaultHubProtocolResolverCreatesProtocolswhenSupoortedProtocolsIsNull(IHubProtocol protocol)
        {
            List<string> supportedProtocols = null;
            var resolver = new DefaultHubProtocolResolver(AllProtocols, NullLogger<DefaultHubProtocolResolver>.Instance);
            Assert.IsType(
                protocol.GetType(),
                resolver.GetProtocol(protocol.Name, supportedProtocols));
        }

        [Theory]
        [MemberData(nameof(HubProtocols))]
        public void DefaultHubProtocolResolverTestsCanCreateSupportedProtocols(IHubProtocol protocol)
        {
            var supportedProtocols = new List<string> { protocol.Name };
            var resolver = new DefaultHubProtocolResolver(AllProtocols, NullLogger<DefaultHubProtocolResolver>.Instance);
            Assert.IsType(
                protocol.GetType(),
                resolver.GetProtocol(protocol.Name, supportedProtocols));
        }

        [Fact]
        public void DefaultHubProtocolResolverThrowsForNullProtocol()
        {
            var resolver = new DefaultHubProtocolResolver(AllProtocols, NullLogger<DefaultHubProtocolResolver>.Instance);
            var exception = Assert.Throws<ArgumentNullException>(
                () => resolver.GetProtocol(null, AllProtocolNames));

            Assert.Equal("protocolName", exception.ParamName);
        }

        [Fact]
        public void DefaultHubProtocolResolverReturnsNullForNotSupportedProtocol()
        {
            var resolver = new DefaultHubProtocolResolver(AllProtocols, NullLogger<DefaultHubProtocolResolver>.Instance);
            Assert.Null(resolver.GetProtocol("notARealProtocol", AllProtocolNames));
        }

        [Fact]
        public void RegisteringMultipleHubProtocolsFails()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => new DefaultHubProtocolResolver(new[] {
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
