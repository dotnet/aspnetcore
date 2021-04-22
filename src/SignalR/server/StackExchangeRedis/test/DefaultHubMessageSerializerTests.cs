// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
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
            var protocolNames = testData.SupportedHubProtocols.ConvertAll(p => p.Name);
            var serializer = new DefaultHubMessageSerializer(resolver, protocolNames, hubSupportedProtocols: null);
            var serializedHubMessage = serializer.SerializeMessage(_testMessage);

            var allBytes = new List<byte>();
            Assert.Equal(testData.SerializedCount, serializedHubMessage.Count);
            foreach (var message in serializedHubMessage)
            {
                allBytes.AddRange(message.Serialized.ToArray());
            }

            Assert.Equal(testData.Encoded, allBytes);
        }

        [Fact]
        public void GlobalSupportedProtocolsOverriddenByHubSupportedProtocols()
        {
            var testData = _invocationTestData["Single supported protocol"];

            var resolver = CreateHubProtocolResolver(new List<IHubProtocol> { new MessagePackHubProtocol(), new JsonHubProtocol() });

            var serializer = new DefaultHubMessageSerializer(resolver, new List<string>() { "json" }, new List<string>() { "messagepack" });
            var serializedHubMessage = serializer.SerializeMessage(_testMessage);

            Assert.Equal(1, serializedHubMessage.Count);

            Assert.Equal(new List<byte>() { 0x0D,
                  0x96,
                    0x01,
                    0x80,
                    0xC0,
                    0xA6, (byte)'t', (byte)'a', (byte)'r', (byte)'g', (byte)'e', (byte)'t',
                    0x90,
                    0x90 },
                    serializedHubMessage[0].Serialized.ToArray());
        }

        private IHubProtocolResolver CreateHubProtocolResolver(List<IHubProtocol> hubProtocols)
        {
            return new DefaultHubProtocolResolver(hubProtocols, NullLogger<DefaultHubProtocolResolver>.Instance);
        }

        private static Dictionary<string, ProtocolTestData> _invocationTestData = new[]
        {
            new ProtocolTestData(
                "Single supported protocol",
                new List<IHubProtocol>() { new MessagePackHubProtocol() },
                1,
                0x0D,
                  0x96,
                    0x01,
                    0x80,
                    0xC0,
                    0xA6, (byte)'t', (byte)'a', (byte)'r', (byte)'g', (byte)'e', (byte)'t',
                    0x90,
                    0x90),
            new ProtocolTestData(
                "Multiple supported protocols",
                new List<IHubProtocol>() { new MessagePackHubProtocol(), new JsonHubProtocol() },
                2,
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
                "Multiple protocols, one not in hub protocol resolver",
                new List<IHubProtocol>() { new MessagePackHubProtocol(), new TestHubProtocol() },
                1,
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
                0)
        }.ToDictionary(t => t.Name);

        public static IEnumerable<object[]> InvocationTestData = _invocationTestData.Keys.Select(k => new object[] { k });

        public class ProtocolTestData
        {
            public string Name { get; }
            public byte[] Encoded { get; }
            public int SerializedCount { get; }
            public List<IHubProtocol> SupportedHubProtocols { get; }

            public ProtocolTestData(string name, List<IHubProtocol> supportedHubProtocols, int serializedCount, params byte[] encoded)
            {
                Name = name;
                Encoded = encoded;
                SerializedCount = serializedCount;
                SupportedHubProtocols = supportedHubProtocols;
            }
        }

        // The actual invocation message doesn't matter
        private static InvocationMessage _testMessage = new InvocationMessage("target", Array.Empty<object>());

        internal class TestHubProtocol : IHubProtocol
        {
            public string Name => "test";

            public int Version => throw new NotImplementedException();

            public TransferFormat TransferFormat => throw new NotImplementedException();

            public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
            {
                throw new NotImplementedException();
            }

            public bool IsVersionSupported(int version)
            {
                throw new NotImplementedException();
            }

            public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
            {
                throw new NotImplementedException();
            }

            public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
            {
                throw new NotImplementedException();
            }
        }
    }
}
