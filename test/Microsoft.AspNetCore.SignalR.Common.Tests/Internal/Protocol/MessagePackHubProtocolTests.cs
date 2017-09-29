// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol
{
    public class MessagePackHubProtocolTests
    {
        private static readonly MessagePackHubProtocol _hubProtocol
            = new MessagePackHubProtocol();

        public static IEnumerable<object[]> TestMessages => new[]
        {
            new object[] { new[] { new InvocationMessage("xyz", /*nonBlocking*/ false, "method") } },
            new object[] { new[] { new InvocationMessage("xyz", /*nonBlocking*/ true, "method") } },
            new object[] { new[] { new InvocationMessage("xyz", /*nonBlocking*/ true, "method", new object[] { null }) } },
            new object[] { new[] { new InvocationMessage("xyz", /*nonBlocking*/ true, "method", 42) } },
            new object[] { new[] { new InvocationMessage("xyz", /*nonBlocking*/ true, "method", 42, "string") } },
            new object[] { new[] { new InvocationMessage("xyz", /*nonBlocking*/ true, "method", 42, "string", new CustomObject()) } },
            new object[] { new[] { new InvocationMessage("xyz", /*nonBlocking*/ true, "method", new[] { new CustomObject(), new CustomObject() }) } },

            new object[] { new[] { new CompletionMessage("xyz", error: "Error not found!", result: null, hasResult: false) } },
            new object[] { new[] { new CompletionMessage("xyz", error: null, result: null, hasResult: false) } },
            new object[] { new[] { new CompletionMessage("xyz", error: null, result: null, hasResult: true) } },
            new object[] { new[] { new CompletionMessage("xyz", error: null, result: 42, hasResult: true) } },
            new object[] { new[] { new CompletionMessage("xyz", error: null, result: 42.0f, hasResult: true) } },
            new object[] { new[] { new CompletionMessage("xyz", error: null, result: "string", hasResult: true) } },
            new object[] { new[] { new CompletionMessage("xyz", error: null, result: true, hasResult: true) } },
            new object[] { new[] { new CompletionMessage("xyz", error: null, result: new CustomObject(), hasResult: true) } },
            new object[] { new[] { new CompletionMessage("xyz", error: null, result: new[] { new CustomObject(), new CustomObject() }, hasResult: true) } },

            new object[] { new[] { new StreamItemMessage("xyz", null) } },
            new object[] { new[] { new StreamItemMessage("xyz", 42) } },
            new object[] { new[] { new StreamItemMessage("xyz", 42.0f) } },
            new object[] { new[] { new StreamItemMessage("xyz", "string") } },
            new object[] { new[] { new StreamItemMessage("xyz", true) } },
            new object[] { new[] { new StreamItemMessage("xyz", new CustomObject()) } },
            new object[] { new[] { new StreamItemMessage("xyz", new[] { new CustomObject(), new CustomObject() }) } },

            new object[]
            {
                new HubMessage[]
                {
                    new InvocationMessage("xyz", /*nonBlocking*/ true, "method", 42, "string", new CustomObject()),
                    new CompletionMessage("xyz", error: null, result: 42, hasResult: true),
                    new StreamItemMessage("xyz", null),
                    new CompletionMessage("xyz", error: null, result: new CustomObject(), hasResult: true)
                }
            }
        };

        [Theory]
        [MemberData(nameof(TestMessages))]
        public void CanRoundTripInvocationMessage(HubMessage[] hubMessages)
        {
            using (var memoryStream = new MemoryStream())
            {
                foreach (var hubMessage in hubMessages)
                {
                    _hubProtocol.WriteMessage(hubMessage, memoryStream);
                }

                _hubProtocol.TryParseMessages(memoryStream.ToArray(), new CompositeTestBinder(hubMessages), out var messages);

                Assert.Equal(hubMessages, messages, TestHubMessageEqualityComparer.Instance);
            }
        }

        public static IEnumerable<object[]> InvalidPayloads => new[]
        {
            new object[] { new byte[0], "Reading array length for 'elementCount' failed." },
            new object[] { new byte[] { 0x91 }, "Reading 'messageType' as Int32 failed." },
            new object[] { new byte[] { 0x91, 0xc2 } , "Reading 'messageType' as Int32 failed." }, // message type is not int
            new object[] { new byte[] { 0x91, 0x0a } , "Invalid message type: 10." },

            // InvocationMessage
            new object[] { new byte[] { 0x95, 0x01 }, "Reading 'invocationId' as String failed." }, // invocationId missing
            new object[] { new byte[] { 0x95, 0x01, 0xc2 }, "Reading 'invocationId' as String failed." }, // 0xc2 is Bool false
            new object[] { new byte[] { 0x95, 0x01, 0xa3, 0x78, 0x79, 0x7a }, "Reading 'nonBlocking' as Boolean failed." }, // nonBlocking missing
            new object[] { new byte[] { 0x95, 0x01, 0xa3, 0x78, 0x79, 0x7a, 0x00 }, "Reading 'nonBlocking' as Boolean failed." }, // nonBlocking is not bool
            new object[] { new byte[] { 0x95, 0x01, 0xa3, 0x78, 0x79, 0x7a, 0xc2 }, "Reading 'target' as String failed." }, // target missing
            new object[] { new byte[] { 0x95, 0x01, 0xa3, 0x78, 0x79, 0x7a, 0xc2, 0x00 }, "Reading 'target' as String failed." }, // 0x00 is Int
            new object[] { new byte[] { 0x95, 0x01, 0xa3, 0x78, 0x79, 0x7a, 0xc2, 0xa1 }, "Reading 'target' as String failed." }, // string is cut
            new object[] { new byte[] { 0x95, 0x01, 0xa3, 0x78, 0x79, 0x7a, 0xc2, 0xa1, 0x78 }, "Reading array length for 'arguments' failed." }, // array is missing
            new object[] { new byte[] { 0x95, 0x01, 0xa3, 0x78, 0x79, 0x7a, 0xc2, 0xa1, 0x78, 0x00 }, "Reading array length for 'arguments' failed." }, // 0x00 is not array marker
            new object[] { new byte[] { 0x95, 0x01, 0xa3, 0x78, 0x79, 0x7a, 0xc2, 0xa1, 0x78, 0x91 }, "Deserializing object of the `String` type for 'argument' failed." }, // array is missing elements
            new object[] { new byte[] { 0x95, 0x01, 0xa3, 0x78, 0x79, 0x7a, 0xc2, 0xa1, 0x78, 0x91, 0xa2, 0x78 }, "Deserializing object of the `String` type for 'argument' failed." }, // array element is cut
            new object[] { new byte[] { 0x95, 0x01, 0xa3, 0x78, 0x79, 0x7a, 0xc2, 0xa1, 0x78, 0x92, 0xa0, 0x00 }, "Target method expects 1 arguments(s) but invocation has 2 argument(s)." }, // argument count does not match binder argument count
            new object[] { new byte[] { 0x95, 0x01, 0xa3, 0x78, 0x79, 0x7a, 0xc2, 0xa1, 0x78, 0x91, 0x00 }, "Deserializing object of the `String` type for 'argument' failed." }, // argument type mismatch

            // StreamItemMessage
            new object[] { new byte[] { 0x93, 0x02 }, "Reading 'invocationId' as String failed." }, // 0xc2 is Bool false
            new object[] { new byte[] { 0x93, 0x02, 0xc2 }, "Reading 'invocationId' as String failed." }, // 0xc2 is Bool false
            new object[] { new byte[] { 0x93, 0x02, 0xa3, 0x78, 0x79, 0x7a }, "Deserializing object of the `String` type for 'item' failed." }, // item is missing
            new object[] { new byte[] { 0x93, 0x02, 0xa3, 0x78, 0x79, 0x7a, 0x00 }, "Deserializing object of the `String` type for 'item' failed." }, // item type mismatch

            // CompletionMessage
            new object[] { new byte[] { 0x93, 0x03 }, "Reading 'invocationId' as String failed." }, // 0xc2 is Bool false
            new object[] { new byte[] { 0x93, 0x03, 0xc2 }, "Reading 'invocationId' as String failed." }, // 0xc2 is Bool false
            new object[] { new byte[] { 0x93, 0x03, 0xa3, 0x78, 0x79, 0x7a, 0xc2 }, "Reading 'resultKind' as Int32 failed." }, // result kind is not int
            new object[] { new byte[] { 0x93, 0x03, 0xa3, 0x78, 0x79, 0x7a, 0x0f }, "Invalid invocation result kind." }, // result kind is out of range
            new object[] { new byte[] { 0x93, 0x03, 0xa3, 0x78, 0x79, 0x7a, 0x01 }, "Reading 'error' as String failed." }, // error result but no error
            new object[] { new byte[] { 0x93, 0x03, 0xa3, 0x78, 0x79, 0x7a, 0x01, 0xa1 }, "Reading 'error' as String failed." }, // error is cut
            new object[] { new byte[] { 0x93, 0x03, 0xa3, 0x78, 0x79, 0x7a, 0x03 }, "Deserializing object of the `String` type for 'argument' failed." }, // non void result but result missing
            new object[] { new byte[] { 0x93, 0x03, 0xa3, 0x78, 0x79, 0x7a, 0x03, 0xa9 }, "Deserializing object of the `String` type for 'argument' failed." }, // result is cut
            new object[] { new byte[] { 0x93, 0x03, 0xa3, 0x78, 0x79, 0x7a, 0x03, 0x00 }, "Deserializing object of the `String` type for 'argument' failed." }, // return type mismatch
        };

        [Theory]
        [MemberData(nameof(InvalidPayloads))]
        public void ParserThrowsForInvalidMessages(byte[] payload, string expectedExceptionMessage)
        {
            var payloadSize = payload.Length;
            Debug.Assert(payloadSize <= 0x7f, "This test does not support payloads larger than 127 bytes");

            // prefix payload with the size
            var buffer = new byte[1 + payloadSize];
            buffer[0] = (byte)(payloadSize & 0x7f);
            Array.Copy(payload, 0, buffer, 1, payloadSize);

            var binder = new TestBinder(new[] { typeof(string) }, typeof(string));
            var exception = Assert.Throws<FormatException>(() => _hubProtocol.TryParseMessages(buffer, binder, out var messages));

            Assert.Equal(expectedExceptionMessage, exception.Message);
        }

        [Theory]
        [InlineData(new object[] { new byte[] { 0x05, 0x01 }, 0 })]
        [InlineData(new object[] {
            new byte[]
            {
                0x05, 0x93, 0x03, 0xa1, 0x78, 0x02,
                0x05, 0x93, 0x03, 0xa1, 0x78, 0x02,
                0x05, 0x93, 0x03, 0xa1
            }, 2 })]
        public void ParserDoesNotConsumePartialData(byte[] payload, int expectedMessagesCount)
        {
            var binder = new TestBinder(new[] { typeof(string) }, typeof(string));
            var result = _hubProtocol.TryParseMessages(payload, binder, out var messages);
            Assert.True(result || messages.Count == 0);
            Assert.Equal(expectedMessagesCount, messages.Count);
        }

        public static IEnumerable<object[]> MessageAndPayload => new object[][]
        {
            new object[]
            {
                new InvocationMessage("0", false, "A", 1, new CustomObject()),
                new byte[]
                {
                    0x5b, 0x95, 0x01, 0xa1, 0x30, 0xc2, 0xa1, 0x41,
                    0x92, // argument array
                    0x01, // 1 - first argument
                    // 0x85 - a map of 5 items (properties)
                    0x85, 0xac, 0x44, 0x61, 0x74, 0x65, 0x54, 0x69, 0x6d, 0x65, 0x50, 0x72, 0x6f, 0x70, 0xd3,
                    0x08, 0xd4, 0x80, 0x6d, 0xb2, 0x76, 0xc0, 0x00, 0xaa, 0x44, 0x6f, 0x75, 0x62, 0x6c, 0x65, 0x50,
                    0x72, 0x6f, 0x70, 0xcb, 0x40, 0x19, 0x21, 0xfb, 0x54, 0x42, 0xcf, 0x12, 0xa7, 0x49, 0x6e, 0x74,
                    0x50, 0x72, 0x6f, 0x70, 0x2a, 0xa8, 0x4e, 0x75, 0x6c, 0x6c, 0x50, 0x72, 0x6f, 0x70, 0xc0, 0xaa,
                    0x53, 0x74, 0x72, 0x69, 0x6e, 0x67, 0x50, 0x72, 0x6f, 0x70, 0xa8, 0x53, 0x69, 0x67, 0x6e, 0x61,
                    0x6c, 0x52, 0x21
                }
            },
            new object[]
            {
                CompletionMessage.WithResult("0", new CustomObject()),
                new byte[]
                {
                    0x57, 0x94, 0x03, 0xa1, 0x30, 0x03,
                    // 0x85 - a map of 5 items (properties)
                    0x85, 0xac, 0x44, 0x61, 0x74, 0x65, 0x54, 0x69, 0x6d, 0x65, 0x50, 0x72, 0x6f, 0x70, 0xd3, 0x08,
                    0xd4, 0x80, 0x6d, 0xb2, 0x76, 0xc0, 0x00, 0xaa, 0x44, 0x6f, 0x75, 0x62, 0x6c, 0x65, 0x50, 0x72,
                    0x6f, 0x70, 0xcb, 0x40, 0x19, 0x21, 0xfb, 0x54, 0x42, 0xcf, 0x12, 0xa7, 0x49, 0x6e, 0x74, 0x50,
                    0x72, 0x6f, 0x70, 0x2a, 0xa8, 0x4e, 0x75, 0x6c, 0x6c, 0x50, 0x72, 0x6f, 0x70, 0xc0, 0xaa, 0x53,
                    0x74, 0x72, 0x69, 0x6e, 0x67, 0x50, 0x72, 0x6f, 0x70, 0xa8, 0x53, 0x69, 0x67, 0x6e, 0x61, 0x6c,
                    0x52, 0x21
                }
            },
            new object[]
            {
                new StreamItemMessage("0", new CustomObject()),
                new byte[]
                {
                    0x56, 0x93, 0x02, 0xa1, 0x30,
                    // 0x85 - a map of 5 items (properties)
                    0x85, 0xac, 0x44, 0x61, 0x74, 0x65, 0x54, 0x69, 0x6d, 0x65, 0x50, 0x72, 0x6f, 0x70, 0xd3, 0x08,
                    0xd4, 0x80, 0x6d, 0xb2, 0x76, 0xc0, 0x00, 0xaa, 0x44, 0x6f, 0x75, 0x62, 0x6c, 0x65, 0x50, 0x72,
                    0x6f, 0x70, 0xcb, 0x40, 0x19, 0x21, 0xfb, 0x54, 0x42, 0xcf, 0x12, 0xa7, 0x49, 0x6e, 0x74, 0x50,
                    0x72, 0x6f, 0x70, 0x2a, 0xa8, 0x4e, 0x75, 0x6c, 0x6c, 0x50, 0x72, 0x6f, 0x70, 0xc0, 0xaa, 0x53,
                    0x74, 0x72, 0x69, 0x6e, 0x67, 0x50, 0x72, 0x6f, 0x70, 0xa8, 0x53, 0x69, 0x67, 0x6e, 0x61, 0x6c,
                    0x52, 0x21
                }
            }
        };

        [Theory]
        [MemberData(nameof(MessageAndPayload))]
        public void UserObjectAreSerializedAsMaps(HubMessage message, byte[] expectedPayload)
        {
            using (var memoryStream = new MemoryStream())
            {
                _hubProtocol.WriteMessage(message, memoryStream);
                Assert.Equal(expectedPayload, memoryStream.ToArray());
            }
        }

        [Fact]
        public void CanWriteObjectsWithoutDefaultCtors()
        {
            var expectedPayload = new byte[] { 0x07, 0x94, 0x03, 0xa1, 0x30, 0x03, 0x91, 0x2a };

            using (var memoryStream = new MemoryStream())
            {
                _hubProtocol.WriteMessage(CompletionMessage.WithResult("0", new List<int> { 42 }.AsReadOnly()), memoryStream);
                Assert.Equal(expectedPayload, memoryStream.ToArray());
            }
        }
    }
}
