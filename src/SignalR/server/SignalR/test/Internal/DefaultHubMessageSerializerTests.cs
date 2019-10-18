// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests.Internal
{
    public class DefaultHubMessageSerializerTests
    {
        [Theory]
        [MemberData(nameof(InvocationTestData))]
        public void SerializeMessages(string testName)
        {
            var testData = _invocationTestData[testName];

            var resolver = CreateHubProtocolResolver(new List<IHubProtocol> { new MessagePackHubProtocol(), new JsonHubProtocol() });
            var protocolNames = new List<string>();
            foreach (var protocol in testData.SupportedHubProtocols)
            {
                protocolNames.Add(protocol.Name);
            }
            var serializer = new DefaultHubMessageSerializer<Hub>(resolver, Options.Create(new HubOptions() { SupportedProtocols = protocolNames, AdditionalHubProtocols = testData.AdditionalHubProtocols }), Options.Create(new HubOptions<Hub>()));
            var serializedHubMessage = serializer.SerializeMessage(_testMessage);

            var serializedMessages = serializedHubMessage.GetAllSerializations().ToList();

            var allBytes = new List<byte>();
            Assert.Equal(testData.SupportedHubProtocols.Count + testData.AdditionalHubProtocols.Count, serializedMessages.Count);
            foreach (var message in serializedMessages)
            {
                allBytes.AddRange(message.Serialized.ToArray());
            }

            Assert.Equal(testData.Encoded, allBytes);
        }

        private IHubProtocolResolver CreateHubProtocolResolver(List<IHubProtocol> hubProtocols)
        {
            return new DefaultHubProtocolResolver(hubProtocols, NullLogger<DefaultHubProtocolResolver>.Instance);
        }

        // We use a func so we are guaranteed to get a new SerializedHubMessage for each test
        private static Dictionary<string, ProtocolTestData> _invocationTestData = new[]
        {
            new ProtocolTestData(
                "Single supported protocol",
                new List<IHubProtocol>() { new MessagePackHubProtocol() },
                new List<IHubProtocol>(),
                0x0D,
                  0x96,
                    0x01,
                    0x80,
                    0xC0,
                    0xA6, (byte)'t', (byte)'a', (byte)'r', (byte)'g', (byte)'e', (byte)'t',
                    0x90,
                    0x90),
            new ProtocolTestData(
                "Supported protocol with same additional protocol",
                new List<IHubProtocol>() { new MessagePackHubProtocol() },
                new List<IHubProtocol>() { new MessagePackHubProtocol() },
                0x0D,
                  0x96,
                    0x01,
                    0x80,
                    0xC0,
                    0xA6, (byte)'t', (byte)'a', (byte)'r', (byte)'g', (byte)'e', (byte)'t',
                    0x90,
                    0x90,
                0x0D,
                  0x96,
                    0x01,
                    0x80,
                    0xC0,
                    0xA6, (byte)'t', (byte)'a', (byte)'r', (byte)'g', (byte)'e', (byte)'t',
                    0x90,
                    0x90),
            new ProtocolTestData(
                "Supported protocol with different additional protocol",
                new List<IHubProtocol>() { new MessagePackHubProtocol() },
                new List<IHubProtocol>() { new JsonHubProtocol() },
                0x0D,
                  0x96,
                    0x01,
                    0x80,
                    0xC0,
                    0xA6, (byte)'t', (byte)'a', (byte)'r', (byte)'g', (byte)'e', (byte)'t',
                    0x90,
                    0x90,
                (byte)'{', (byte)'"', (byte)'t', (byte)'y', (byte)'p', (byte)'e', (byte)'"', (byte)':', (byte)'1',
                (byte)',',(byte)'"', (byte)'t', (byte)'a', (byte)'r', (byte)'g', (byte)'e', (byte)'t', (byte)'"', (byte)':',
                (byte)'"', (byte)'t', (byte)'a', (byte)'r', (byte)'g', (byte)'e', (byte)'t', (byte)'"',
                (byte)',', (byte)'"', (byte)'a', (byte)'r', (byte)'g', (byte)'u', (byte)'m', (byte)'e', (byte)'n', (byte)'t', (byte)'s', (byte)'"',
                (byte)':', (byte)'[', (byte)']', (byte)'}', 0x1e),
            new ProtocolTestData(
                "No supported protocol with additional protocol",
                new List<IHubProtocol>(),
                new List<IHubProtocol>() { new MessagePackHubProtocol() },
                0x0D,
                  0x96,
                    0x01,
                    0x80,
                    0xC0,
                    0xA6, (byte)'t', (byte)'a', (byte)'r', (byte)'g', (byte)'e', (byte)'t',
                    0x90,
                    0x90),
            new ProtocolTestData(
                "No protocols",
                new List<IHubProtocol>(),
                new List<IHubProtocol>()),
        }.ToDictionary(t => t.Name);

        public static IEnumerable<object[]> InvocationTestData = _invocationTestData.Keys.Select(k => new object[] { k });

        public class ProtocolTestData
        {
            public string Name { get; }
            public byte[] Encoded { get; }
            public List<IHubProtocol> AdditionalHubProtocols { get; }
            public List<IHubProtocol> SupportedHubProtocols { get; }

            public ProtocolTestData(string name, List<IHubProtocol> supportedHubProtocols, List<IHubProtocol> additionalHubProtocols, params byte[] encoded)
            {
                Name = name;
                Encoded = encoded;
                AdditionalHubProtocols = additionalHubProtocols;
                SupportedHubProtocols = supportedHubProtocols;
            }
        }

        // The actual invocation message doesn't matter
        private static InvocationMessage _testMessage = new InvocationMessage("target", Array.Empty<object>());
    }
}
