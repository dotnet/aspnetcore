// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.StackExchangeRedis.Internal;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Tests
{
    public class RedisProtocolTests
    {
        private static Dictionary<string, ProtocolTestData<int>> _ackTestData = new[]
        {
            CreateTestData("Zero", 0, 0x91, 0x00),
            CreateTestData("Fixnum", 42, 0x91, 0x2A),
            CreateTestData("Uint8", 180, 0x91, 0xCC, 0xB4),
            CreateTestData("Uint16", 384, 0x91, 0xCD, 0x01, 0x80),
            CreateTestData("Uint32", 70_000, 0x91, 0xCE, 0x00, 0x01, 0x11, 0x70),
        }.ToDictionary(t => t.Name);

        public static IEnumerable<object[]> AckTestData = _ackTestData.Keys.Select(k => new object[] { k });

        [Theory]
        [MemberData(nameof(AckTestData))]
        public void ParseAck(string testName)
        {
            var testData = _ackTestData[testName];
            var protocol = new RedisProtocol(CreateHubMessageSerializer(new List<IHubProtocol>()));

            var decoded = protocol.ReadAck(testData.Encoded);

            Assert.Equal(testData.Decoded, decoded);
        }

        [Theory]
        [MemberData(nameof(AckTestData))]
        public void WriteAck(string testName)
        {
            var testData = _ackTestData[testName];
            var protocol = new RedisProtocol(CreateHubMessageSerializer(new List<IHubProtocol>()));

            var encoded = protocol.WriteAck(testData.Decoded);

            Assert.Equal(testData.Encoded, encoded);
        }

        private static Dictionary<string, ProtocolTestData<RedisGroupCommand>> _groupCommandTestData = new[]
        {
            CreateTestData("GroupAdd", new RedisGroupCommand(42, "S", GroupAction.Add, "G", "C" ), 0x95, 0x2A, 0xA1, (byte)'S', 0x01, 0xA1, (byte)'G', 0xA1, (byte)'C'),
            CreateTestData("GroupRemove", new RedisGroupCommand(42, "S", GroupAction.Remove, "G", "C" ), 0x95, 0x2A, 0xA1, (byte)'S', 0x02, 0xA1, (byte)'G', 0xA1, (byte)'C'),
        }.ToDictionary(t => t.Name);

        public static IEnumerable<object[]> GroupCommandTestData = _groupCommandTestData.Keys.Select(k => new object[] { k });

        [Theory]
        [MemberData(nameof(GroupCommandTestData))]
        public void ParseGroupCommand(string testName)
        {
            var testData = _groupCommandTestData[testName];
            var protocol = new RedisProtocol(CreateHubMessageSerializer(new List<IHubProtocol>()));

            var decoded = protocol.ReadGroupCommand(testData.Encoded);

            Assert.Equal(testData.Decoded.Id, decoded.Id);
            Assert.Equal(testData.Decoded.ServerName, decoded.ServerName);
            Assert.Equal(testData.Decoded.Action, decoded.Action);
            Assert.Equal(testData.Decoded.GroupName, decoded.GroupName);
            Assert.Equal(testData.Decoded.ConnectionId, decoded.ConnectionId);
        }

        [Theory]
        [MemberData(nameof(GroupCommandTestData))]
        public void WriteGroupCommand(string testName)
        {
            var testData = _groupCommandTestData[testName];
            var protocol = new RedisProtocol(CreateHubMessageSerializer(new List<IHubProtocol>()));

            var encoded = protocol.WriteGroupCommand(testData.Decoded);

            Assert.Equal(testData.Encoded, encoded);
        }

        // The actual invocation message doesn't matter
        private static InvocationMessage _testMessage = new InvocationMessage("target", Array.Empty<object>());

        // We use a func so we are guaranteed to get a new SerializedHubMessage for each test
        private static Dictionary<string, ProtocolTestData<Func<RedisInvocation>>> _invocationTestData = new[]
        {
            CreateTestData<Func<RedisInvocation>>(
                "NoExcludedIds",
                () => new RedisInvocation(new SerializedHubMessage(_testMessage), null),
                0x92,
                    0x90,
                    0x82,
                        0xA2, (byte)'p', (byte)'1',
                        0xC4, 0x01, 0x2A,
                        0xA2, (byte)'p', (byte)'2',
                        0xC4, 0x01, 0x2A),
            CreateTestData<Func<RedisInvocation>>(
                "OneExcludedId",
                () => new RedisInvocation(new SerializedHubMessage(_testMessage), new [] { "a" }),
                0x92,
                    0x91,
                        0xA1, (byte)'a',
                    0x82,
                        0xA2, (byte)'p', (byte)'1',
                        0xC4, 0x01, 0x2A,
                        0xA2, (byte)'p', (byte)'2',
                        0xC4, 0x01, 0x2A),
            CreateTestData<Func<RedisInvocation>>(
                "ManyExcludedIds",
                () => new RedisInvocation(new SerializedHubMessage(_testMessage), new [] { "a", "b", "c", "d", "e", "f" }),
                0x92,
                    0x96,
                        0xA1, (byte)'a',
                        0xA1, (byte)'b',
                        0xA1, (byte)'c',
                        0xA1, (byte)'d',
                        0xA1, (byte)'e',
                        0xA1, (byte)'f',
                    0x82,
                        0xA2, (byte)'p', (byte)'1',
                        0xC4, 0x01, 0x2A,
                        0xA2, (byte)'p', (byte)'2',
                        0xC4, 0x01, 0x2A),
        }.ToDictionary(t => t.Name);

        public static IEnumerable<object[]> InvocationTestData = _invocationTestData.Keys.Select(k => new object[] { k });

        [Theory]
        [MemberData(nameof(InvocationTestData))]
        public void ParseInvocation(string testName)
        {
            var testData = _invocationTestData[testName];
            var hubProtocols = new[] { new DummyHubProtocol("p1"), new DummyHubProtocol("p2") };
            var protocol = new RedisProtocol(CreateHubMessageSerializer(hubProtocols.Cast<IHubProtocol>().ToList()));

            var expected = testData.Decoded();

            var decoded = protocol.ReadInvocation(testData.Encoded);

            Assert.Equal(expected.ExcludedConnectionIds, decoded.ExcludedConnectionIds);

            // Verify the deserialized object has the necessary serialized forms
            foreach (var hubProtocol in hubProtocols)
            {
                Assert.Equal(
                    expected.Message.GetSerializedMessage(hubProtocol).ToArray(),
                    decoded.Message.GetSerializedMessage(hubProtocol).ToArray());

                var writtenMessages = hubProtocol.GetWrittenMessages();
                Assert.Collection(writtenMessages,
                    actualMessage =>
                    {
                        var invocation = Assert.IsType<InvocationMessage>(actualMessage);
                        Assert.Same(_testMessage.Target, invocation.Target);
                        Assert.Same(_testMessage.Arguments, invocation.Arguments);
                    });
            }
        }

        [Theory]
        [MemberData(nameof(InvocationTestData))]
        public void WriteInvocation(string testName)
        {
            var testData = _invocationTestData[testName];
            var protocol = new RedisProtocol(CreateHubMessageSerializer(new List<IHubProtocol>() { new DummyHubProtocol("p1"), new DummyHubProtocol("p2") }));

            // Actual invocation doesn't matter because we're using a dummy hub protocol.
            // But the dummy protocol will check that we gave it the test message to make sure everything flows through properly.
            var expected = testData.Decoded();
            var encoded = protocol.WriteInvocation(_testMessage.Target, _testMessage.Arguments, expected.ExcludedConnectionIds);

            Assert.Equal(testData.Encoded, encoded);
        }

        [Theory]
        [MemberData(nameof(InvocationTestData))]
        public void WriteInvocationWithHubMessageSerializer(string testName)
        {
            var testData = _invocationTestData[testName];
            var hubMessageSerializer = CreateHubMessageSerializer(new List<IHubProtocol>() { new DummyHubProtocol("p1"), new DummyHubProtocol("p2") });
            var protocol = new RedisProtocol(hubMessageSerializer);

            // Actual invocation doesn't matter because we're using a dummy hub protocol.
            // But the dummy protocol will check that we gave it the test message to make sure everything flows through properly.
            var expected = testData.Decoded();
            var encoded = protocol.WriteInvocation(_testMessage.Target, _testMessage.Arguments, expected.ExcludedConnectionIds);

            Assert.Equal(testData.Encoded, encoded);
        }

        // Create ProtocolTestData<T> using the Power of Type Inference(TM).
        private static ProtocolTestData<T> CreateTestData<T>(string name, T decoded, params byte[] encoded)
            => new ProtocolTestData<T>(name, decoded, encoded);

        public class ProtocolTestData<T>
        {
            public string Name { get; }
            public T Decoded { get; }
            public byte[] Encoded { get; }

            public ProtocolTestData(string name, T decoded, byte[] encoded)
            {
                Name = name;
                Decoded = decoded;
                Encoded = encoded;
            }
        }

        private DefaultHubMessageSerializer CreateHubMessageSerializer(List<IHubProtocol> protocols)
        {
            var protocolResolver = new DefaultHubProtocolResolver(protocols, NullLogger<DefaultHubProtocolResolver>.Instance);

            return new DefaultHubMessageSerializer(protocolResolver, protocols.ConvertAll(p => p.Name), hubSupportedProtocols: null);
        }
    }
}
