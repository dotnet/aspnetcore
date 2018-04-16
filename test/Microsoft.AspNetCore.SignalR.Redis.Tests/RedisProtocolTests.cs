using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Redis.Internal;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Redis.Tests
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
            var protocol = new RedisProtocol(Array.Empty<IHubProtocol>());

            var decoded = protocol.ReadAck(testData.Encoded);

            Assert.Equal(testData.Decoded, decoded);
        }

        [Theory]
        [MemberData(nameof(AckTestData))]
        public void WriteAck(string testName)
        {
            var testData = _ackTestData[testName];
            var protocol = new RedisProtocol(Array.Empty<IHubProtocol>());

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
            var protocol = new RedisProtocol(Array.Empty<IHubProtocol>());

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
            var protocol = new RedisProtocol(Array.Empty<IHubProtocol>());

            var encoded = protocol.WriteGroupCommand(testData.Decoded);

            Assert.Equal(testData.Encoded, encoded);
        }

        // The actual invocation message doesn't matter
        private static InvocationMessage _testMessage = new InvocationMessage("target", null, Array.Empty<object>());
        private static Dictionary<string, ProtocolTestData<RedisInvocation>> _invocationTestData = new[]
        {
            CreateTestData(
                "NoExcludedIds",
                new RedisInvocation(new SerializedHubMessage(_testMessage), null),
                0x92,
                    0x90,
                    0x82,
                        0xA2, (byte)'p', (byte)'1',
                        0xC4, 0x01, 0x2A,
                        0xA2, (byte)'p', (byte)'2',
                        0xC4, 0x01, 0x2A),
            CreateTestData(
                "OneExcludedId",
                new RedisInvocation(new SerializedHubMessage(_testMessage), new [] { "a" }),
                0x92,
                    0x91,
                        0xA1, (byte)'a',
                    0x82,
                        0xA2, (byte)'p', (byte)'1',
                        0xC4, 0x01, 0x2A,
                        0xA2, (byte)'p', (byte)'2',
                        0xC4, 0x01, 0x2A),
            CreateTestData(
                "ManyExcludedIds",
                new RedisInvocation(new SerializedHubMessage(_testMessage), new [] { "a", "b", "c", "d", "e", "f" }),
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
            var protocol = new RedisProtocol(hubProtocols);

            var decoded = protocol.ReadInvocation(testData.Encoded);

            Assert.Equal(testData.Decoded.ExcludedConnectionIds, decoded.ExcludedConnectionIds);

            // Verify the deserialized object has the necessary serialized forms
            foreach (var hubProtocol in hubProtocols)
            {
                Assert.Equal(
                    testData.Decoded.Message.GetSerializedMessage(hubProtocol).ToArray(),
                    decoded.Message.GetSerializedMessage(hubProtocol).ToArray());
                Assert.Equal(1, hubProtocol.SerializationCount);
            }
        }

        [Theory]
        [MemberData(nameof(InvocationTestData))]
        public void WriteInvocation(string testName)
        {
            var testData = _invocationTestData[testName];
            var protocol = new RedisProtocol(new[] { new DummyHubProtocol("p1"), new DummyHubProtocol("p2") });

            // Actual invocation doesn't matter because we're using a dummy hub protocol.
            // But the dummy protocol will check that we gave it the test message to make sure everything flows through properly.
            var encoded = protocol.WriteInvocation(_testMessage.Target, _testMessage.Arguments, testData.Decoded.ExcludedConnectionIds);

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

        public class DummyHubProtocol : IHubProtocol
        {
            public int SerializationCount { get; private set; }

            public string Name { get; }
            public int Version => 1;
            public TransferFormat TransferFormat => TransferFormat.Text;

            public DummyHubProtocol(string name)
            {
                Name = name;
            }

            public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
            {
                throw new NotSupportedException();
            }

            public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
            {
                output.Write(GetMessageBytes(message).Span);
            }

            public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
            {
                SerializationCount += 1;

                // Assert that we got the test message
                var invocation = Assert.IsType<InvocationMessage>(message);
                Assert.Same(_testMessage.Target, invocation.Target);
                Assert.Same(_testMessage.Arguments, invocation.Arguments);

                return new byte[] { 0x2A };
            }

            public bool IsVersionSupported(int version)
            {
                throw new NotSupportedException();
            }
        }
    }
}
