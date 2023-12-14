// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.StackExchangeRedis.Internal;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Tests;

public class RedisProtocolTests
{
    private static readonly Dictionary<string, ProtocolTestData<int>> _ackTestData = new[]
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

        var decoded = RedisProtocol.ReadAck(testData.Encoded);

        Assert.Equal(testData.Decoded, decoded);
    }

    [Theory]
    [MemberData(nameof(AckTestData))]
    public void WriteAck(string testName)
    {
        var testData = _ackTestData[testName];
        var protocol = new RedisProtocol(CreateHubMessageSerializer(new List<IHubProtocol>()));

        var encoded = RedisProtocol.WriteAck(testData.Decoded);

        Assert.Equal(testData.Encoded, encoded);
    }

    private static readonly Dictionary<string, ProtocolTestData<RedisGroupCommand>> _groupCommandTestData = new[]
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

        var decoded = RedisProtocol.ReadGroupCommand(testData.Encoded);

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

        var encoded = RedisProtocol.WriteGroupCommand(testData.Decoded);

        Assert.Equal(testData.Encoded, encoded);
    }

    // The actual invocation message doesn't matter
    private static readonly InvocationMessage _testMessage = new InvocationMessage("target", Array.Empty<object>());

    // We use a func so we are guaranteed to get a new SerializedHubMessage for each test
    private static readonly Dictionary<string, ProtocolTestData<Func<RedisInvocation>>> _invocationTestData = new[]
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

        var decoded = RedisProtocol.ReadInvocation(testData.Encoded);

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
        var encoded = protocol.WriteInvocation(_testMessage.Target, _testMessage.Arguments, excludedConnectionIds: expected.ExcludedConnectionIds);

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
        var encoded = protocol.WriteInvocation(_testMessage.Target, _testMessage.Arguments, excludedConnectionIds: expected.ExcludedConnectionIds);

        Assert.Equal(testData.Encoded, encoded);
    }

    private static readonly Dictionary<string, ProtocolTestData<RedisCompletion>> _completionMessageTestData = new[]
    {
            CreateTestData<RedisCompletion>(
                "JsonMessageForwarded",
                new RedisCompletion("json", new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("{\"type\":3,\"invocationId\":\"1\",\"result\":1}"))),
                0x92,
                    0xa4,
                        (byte)'j',
                        (byte)'s',
                        (byte)'o',
                        (byte)'n',
                    0xc4, // 'bin'
                        0x28, // length
                            0x7b, 0x22, 0x74, 0x79, 0x70, 0x65, 0x22, 0x3a, 0x33, 0x2c, 0x22, 0x69, 0x6e, 0x76, 0x6f, 0x63, 0x61, 0x74, 0x69,
                            0x6f, 0x6e, 0x49, 0x64, 0x22, 0x3a, 0x22, 0x31, 0x22, 0x2c, 0x22, 0x72, 0x65, 0x73, 0x75, 0x6c, 0x74, 0x22, 0x3a,
                            0x31, 0x7d),
            CreateTestData<RedisCompletion>(
                "MsgPackMessageForwarded",
                new RedisCompletion("messagepack", new ReadOnlySequence<byte>(new byte[] { 0x95, 0x03, 0x80, 0xa3, (byte)'x', (byte)'y', (byte)'z', 0x03, 0x2a })),
                0x92,
                    0xab,
                        (byte)'m',
                        (byte)'e',
                        (byte)'s',
                        (byte)'s',
                        (byte)'a',
                        (byte)'g',
                        (byte)'e',
                        (byte)'p',
                        (byte)'a',
                        (byte)'c',
                        (byte)'k',
                    0xc4, // 'bin'
                        0x09, // 'bin' length
                            0x95, // 5 array elements
                                0x03, // type: 3
                                0x80, // empty headers
                                0xa3, // 'str'
                                    (byte)'x',
                                    (byte)'y',
                                    (byte)'z',
                                0x03, // has result
                                0x2a), // 42
        }.ToDictionary(t => t.Name);

    public static IEnumerable<object[]> CompletionMessageTestData = _completionMessageTestData.Keys.Select(k => new object[] { k });

    [Theory]
    [MemberData(nameof(CompletionMessageTestData))]
    public void ParseCompletionMessage(string testName)
    {
        var testData = _completionMessageTestData[testName];

        var completionMessage = RedisProtocol.ReadCompletion(testData.Encoded);

        Assert.Equal(testData.Decoded.ProtocolName, completionMessage.ProtocolName);
        Assert.Equal(testData.Decoded.CompletionMessage.ToArray(), completionMessage.CompletionMessage.ToArray());
    }

    [Theory]
    [MemberData(nameof(CompletionMessageTestData))]
    public void WriteCompletionMessage(string testName)
    {
        var testData = _completionMessageTestData[testName];

        var writer = MemoryBufferWriter.Get();
        writer.Write(testData.Decoded.CompletionMessage.ToArray());

        var encoded = RedisProtocol.WriteCompletionMessage(writer, testData.Decoded.ProtocolName);
        MemoryBufferWriter.Return(writer);

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
