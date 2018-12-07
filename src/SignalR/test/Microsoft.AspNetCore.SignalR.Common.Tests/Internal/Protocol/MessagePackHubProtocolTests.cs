// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Internal;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol
{
    using Microsoft.AspNetCore.SignalR.Protocol;
    using static HubMessageHelpers;

    public class MessagePackHubProtocolTests
    {
        private static readonly IDictionary<string, string> TestHeaders = new Dictionary<string, string>
        {
            { "Foo", "Bar" },
            { "KeyWith\nNew\r\nLines", "Still Works" },
            { "ValueWithNewLines", "Also\nWorks\r\nFine" },
        };

        private static readonly MessagePackHubProtocol _hubProtocol
            = new MessagePackHubProtocol();

        public enum TestEnum
        {
            Zero = 0,
            One
        }

        // Test Data for Parse/WriteMessages:
        // * Name: A string name that is used when reporting the test (it's the ToString value for ProtocolTestData)
        // * Message: The HubMessage that is either expected (in Parse) or used as input (in Write)
        // * Binary: Base64-encoded binary "baseline" to sanity-check MessagePack-CSharp behavior
        //
        // When changing the tests/message pack parsing if you get test failures look at the base64 encoding and
        // use a tool like https://sugendran.github.io/msgpack-visualizer/ to verify that the MsgPack is correct and then just replace the Base64 value.

        public static IEnumerable<object[]> TestDataNames
        {
            get
            {
                foreach (var k in TestData.Keys)
                {
                    yield return new object[] { k };
                }
            }
        }

        public static IDictionary<string, ProtocolTestData> TestData => new[]
        {
            // Invocation messages
            new ProtocolTestData(
                name: "InvocationWithNoHeadersAndNoArgs",
                message: new InvocationMessage("xyz", "method", Array.Empty<object>()),
                binary: "lQGAo3h5eqZtZXRob2SQ"),
            new ProtocolTestData(
                name: "InvocationWithNoHeadersNoIdAndNoArgs",
                message: new InvocationMessage("method", Array.Empty<object>()),
                binary: "lQGAwKZtZXRob2SQ"),
            new ProtocolTestData(
                name: "InvocationWithNoHeadersNoIdAndSingleNullArg",
                message: new InvocationMessage("method", new object[] { null }),
                binary: "lQGAwKZtZXRob2SRwA=="),
            new ProtocolTestData(
                name: "InvocationWithNoHeadersNoIdAndSingleIntArg",
                message: new InvocationMessage("method", new object[] { 42 }),
                binary: "lQGAwKZtZXRob2SRKg=="),
            new ProtocolTestData(
                name: "InvocationWithNoHeadersNoIdIntAndStringArgs",
                message: new InvocationMessage("method", new object[] { 42, "string" }),
                binary: "lQGAwKZtZXRob2SSKqZzdHJpbmc="),
            new ProtocolTestData(
                name: "InvocationWithNoHeadersNoIdIntAndEnumArgs",
                message: new InvocationMessage("method", new object[] { 42, TestEnum.One }),
                binary: "lQGAwKZtZXRob2SSKqNPbmU="),
            new ProtocolTestData(
                name: "InvocationWithNoHeadersNoIdAndCustomObjectArg",
                message: new InvocationMessage("method", new object[] { 42, "string", new CustomObject() }),
                binary: "lQGAwKZtZXRob2STKqZzdHJpbmeGqlN0cmluZ1Byb3CoU2lnbmFsUiGqRG91YmxlUHJvcMtAGSH7VELPEqdJbnRQcm9wKqxEYXRlVGltZVByb3DW/1jsHICoTnVsbFByb3DAq0J5dGVBcnJQcm9wxAMBAgM="),
            new ProtocolTestData(
                name: "InvocationWithNoHeadersNoIdAndArrayOfCustomObjectArgs",
                message: new InvocationMessage("method", new object[] { new CustomObject(), new CustomObject() }),
                binary: "lQGAwKZtZXRob2SShqpTdHJpbmdQcm9wqFNpZ25hbFIhqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqsRGF0ZVRpbWVQcm9w1v9Y7ByAqE51bGxQcm9wwKtCeXRlQXJyUHJvcMQDAQIDhqpTdHJpbmdQcm9wqFNpZ25hbFIhqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqsRGF0ZVRpbWVQcm9w1v9Y7ByAqE51bGxQcm9wwKtCeXRlQXJyUHJvcMQDAQID"),
            new ProtocolTestData(
                name: "InvocationWithHeadersNoIdAndArrayOfCustomObjectArgs",
                message: AddHeaders(TestHeaders, new InvocationMessage("method", new object[] { new CustomObject(), new CustomObject() })),
                binary: "lQGDo0Zvb6NCYXKyS2V5V2l0aApOZXcNCkxpbmVzq1N0aWxsIFdvcmtzsVZhbHVlV2l0aE5ld0xpbmVzsEFsc28KV29ya3MNCkZpbmXApm1ldGhvZJKGqlN0cmluZ1Byb3CoU2lnbmFsUiGqRG91YmxlUHJvcMtAGSH7VELPEqdJbnRQcm9wKqxEYXRlVGltZVByb3DW/1jsHICoTnVsbFByb3DAq0J5dGVBcnJQcm9wxAMBAgOGqlN0cmluZ1Byb3CoU2lnbmFsUiGqRG91YmxlUHJvcMtAGSH7VELPEqdJbnRQcm9wKqxEYXRlVGltZVByb3DW/1jsHICoTnVsbFByb3DAq0J5dGVBcnJQcm9wxAMBAgM="),
            new ProtocolTestData(
                name: "InvocationWithStreamPlaceholderObject",
                message: new InvocationMessage(null, "Target", new object[] { new StreamPlaceholder("__test_id__")}),
                binary: "lQGAwKZUYXJnZXSRgahTdHJlYW1JZKtfX3Rlc3RfaWRfXw=="
                ),

            // StreamItem Messages
            new ProtocolTestData(
                name: "StreamItemWithNoHeadersAndNullItem",
                message: new StreamItemMessage("xyz", item: null),
                binary: "lAKAo3h5esA="),
            new ProtocolTestData(
                name: "StreamItemWithNoHeadersAndIntItem",
                message: new StreamItemMessage("xyz", item: 42),
                binary: "lAKAo3h5eio="),
            new ProtocolTestData(
                name: "StreamItemWithNoHeadersAndFloatItem",
                message: new StreamItemMessage("xyz", item: 42.0f),
                binary: "lAKAo3h5espCKAAA"),
            new ProtocolTestData(
                name: "StreamItemWithNoHeadersAndStringItem",
                message: new StreamItemMessage("xyz", item: "string"),
                binary: "lAKAo3h5eqZzdHJpbmc="),
            new ProtocolTestData(
                name: "StreamItemWithNoHeadersAndBoolItem",
                message: new StreamItemMessage("xyz", item: true),
                binary: "lAKAo3h5esM="),
            new ProtocolTestData(
                name: "StreamItemWithNoHeadersAndEnumItem",
                message: new StreamItemMessage("xyz", item: TestEnum.One),
                binary: "lAKAo3h5eqNPbmU="),
            new ProtocolTestData(
                name: "StreamItemWithNoHeadersAndCustomObjectItem",
                message: new StreamItemMessage("xyz", item: new CustomObject()),
                binary: "lAKAo3h5eoaqU3RyaW5nUHJvcKhTaWduYWxSIapEb3VibGVQcm9wy0AZIftUQs8Sp0ludFByb3AqrERhdGVUaW1lUHJvcNb/WOwcgKhOdWxsUHJvcMCrQnl0ZUFyclByb3DEAwECAw=="),
            new ProtocolTestData(
                name: "StreamItemWithNoHeadersAndCustomObjectArrayItem",
                message: new StreamItemMessage("xyz", item: new[] { new CustomObject(), new CustomObject() }),
                binary: "lAKAo3h5epKGqlN0cmluZ1Byb3CoU2lnbmFsUiGqRG91YmxlUHJvcMtAGSH7VELPEqdJbnRQcm9wKqxEYXRlVGltZVByb3DW/1jsHICoTnVsbFByb3DAq0J5dGVBcnJQcm9wxAMBAgOGqlN0cmluZ1Byb3CoU2lnbmFsUiGqRG91YmxlUHJvcMtAGSH7VELPEqdJbnRQcm9wKqxEYXRlVGltZVByb3DW/1jsHICoTnVsbFByb3DAq0J5dGVBcnJQcm9wxAMBAgM="),
            new ProtocolTestData(
                name: "StreamItemWithHeadersAndCustomObjectArrayItem",
                message: AddHeaders(TestHeaders, new StreamItemMessage("xyz", item: new[] { new CustomObject(), new CustomObject() })),
                binary: "lAKDo0Zvb6NCYXKyS2V5V2l0aApOZXcNCkxpbmVzq1N0aWxsIFdvcmtzsVZhbHVlV2l0aE5ld0xpbmVzsEFsc28KV29ya3MNCkZpbmWjeHl6koaqU3RyaW5nUHJvcKhTaWduYWxSIapEb3VibGVQcm9wy0AZIftUQs8Sp0ludFByb3AqrERhdGVUaW1lUHJvcNb/WOwcgKhOdWxsUHJvcMCrQnl0ZUFyclByb3DEAwECA4aqU3RyaW5nUHJvcKhTaWduYWxSIapEb3VibGVQcm9wy0AZIftUQs8Sp0ludFByb3AqrERhdGVUaW1lUHJvcNb/WOwcgKhOdWxsUHJvcMCrQnl0ZUFyclByb3DEAwECAw=="),

            // Completion Messages
            new ProtocolTestData(
                name: "CompletionWithNoHeadersAndError",
                message: CompletionMessage.WithError("xyz", error: "Error not found!"),
                binary: "lQOAo3h5egGwRXJyb3Igbm90IGZvdW5kIQ=="),
            new ProtocolTestData(
                name: "CompletionWithHeadersAndError",
                message: AddHeaders(TestHeaders, CompletionMessage.WithError("xyz", error: "Error not found!")),
                binary: "lQODo0Zvb6NCYXKyS2V5V2l0aApOZXcNCkxpbmVzq1N0aWxsIFdvcmtzsVZhbHVlV2l0aE5ld0xpbmVzsEFsc28KV29ya3MNCkZpbmWjeHl6AbBFcnJvciBub3QgZm91bmQh"),
            new ProtocolTestData(
                name: "CompletionWithNoHeadersAndNoResult",
                message: CompletionMessage.Empty("xyz"),
                binary: "lAOAo3h5egI="),
            new ProtocolTestData(
                name: "CompletionWithHeadersAndNoResult",
                message: AddHeaders(TestHeaders, CompletionMessage.Empty("xyz")),
                binary: "lAODo0Zvb6NCYXKyS2V5V2l0aApOZXcNCkxpbmVzq1N0aWxsIFdvcmtzsVZhbHVlV2l0aE5ld0xpbmVzsEFsc28KV29ya3MNCkZpbmWjeHl6Ag=="),
            new ProtocolTestData(
                name: "CompletionWithNoHeadersAndNullResult",
                message: CompletionMessage.WithResult("xyz", payload: null),
                binary: "lQOAo3h5egPA"),
            new ProtocolTestData(
                name: "CompletionWithNoHeadersAndIntResult",
                message: CompletionMessage.WithResult("xyz", payload: 42),
                binary: "lQOAo3h5egMq"),
            new ProtocolTestData(
                name: "CompletionWithNoHeadersAndEnumResult",
                message: CompletionMessage.WithResult("xyz", payload: TestEnum.One),
                binary: "lQOAo3h5egOjT25l"),
            new ProtocolTestData(
                name: "CompletionWithNoHeadersAndFloatResult",
                message: CompletionMessage.WithResult("xyz", payload: 42.0f),
                binary: "lQOAo3h5egPKQigAAA=="),
            new ProtocolTestData(
                name: "CompletionWithNoHeadersAndStringResult",
                message: CompletionMessage.WithResult("xyz", payload: "string"),
                binary: "lQOAo3h5egOmc3RyaW5n"),
            new ProtocolTestData(
                name: "CompletionWithNoHeadersAndBooleanResult",
                message: CompletionMessage.WithResult("xyz", payload: true),
                binary: "lQOAo3h5egPD"),
            new ProtocolTestData(
                name: "CompletionWithNoHeadersAndCustomObjectResult",
                message: CompletionMessage.WithResult("xyz", payload: new CustomObject()),
                binary: "lQOAo3h5egOGqlN0cmluZ1Byb3CoU2lnbmFsUiGqRG91YmxlUHJvcMtAGSH7VELPEqdJbnRQcm9wKqxEYXRlVGltZVByb3DW/1jsHICoTnVsbFByb3DAq0J5dGVBcnJQcm9wxAMBAgM="),
            new ProtocolTestData(
                name: "CompletionWithNoHeadersAndCustomObjectArrayResult",
                message: CompletionMessage.WithResult("xyz", payload: new[] { new CustomObject(), new CustomObject() }),
                binary: "lQOAo3h5egOShqpTdHJpbmdQcm9wqFNpZ25hbFIhqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqsRGF0ZVRpbWVQcm9w1v9Y7ByAqE51bGxQcm9wwKtCeXRlQXJyUHJvcMQDAQIDhqpTdHJpbmdQcm9wqFNpZ25hbFIhqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqsRGF0ZVRpbWVQcm9w1v9Y7ByAqE51bGxQcm9wwKtCeXRlQXJyUHJvcMQDAQID"),
            new ProtocolTestData(
                name: "CompletionWithHeadersAndCustomObjectArrayResult",
                message: AddHeaders(TestHeaders, CompletionMessage.WithResult("xyz", payload: new[] { new CustomObject(), new CustomObject() })),
                binary: "lQODo0Zvb6NCYXKyS2V5V2l0aApOZXcNCkxpbmVzq1N0aWxsIFdvcmtzsVZhbHVlV2l0aE5ld0xpbmVzsEFsc28KV29ya3MNCkZpbmWjeHl6A5KGqlN0cmluZ1Byb3CoU2lnbmFsUiGqRG91YmxlUHJvcMtAGSH7VELPEqdJbnRQcm9wKqxEYXRlVGltZVByb3DW/1jsHICoTnVsbFByb3DAq0J5dGVBcnJQcm9wxAMBAgOGqlN0cmluZ1Byb3CoU2lnbmFsUiGqRG91YmxlUHJvcMtAGSH7VELPEqdJbnRQcm9wKqxEYXRlVGltZVByb3DW/1jsHICoTnVsbFByb3DAq0J5dGVBcnJQcm9wxAMBAgM="),

            // StreamInvocation Messages
            new ProtocolTestData(
                name: "StreamInvocationWithNoHeadersAndNoArgs",
                message: new StreamInvocationMessage("xyz", "method", Array.Empty<object>()),
                binary: "lQSAo3h5eqZtZXRob2SQ"),
            new ProtocolTestData(
                name: "StreamInvocationWithNoHeadersAndNullArg",
                message: new StreamInvocationMessage("xyz", "method", new object[] { null }),
                binary: "lQSAo3h5eqZtZXRob2SRwA=="),
            new ProtocolTestData(
                name: "StreamInvocationWithNoHeadersAndIntArg",
                message: new StreamInvocationMessage("xyz", "method", new object[] { 42 }),
                binary: "lQSAo3h5eqZtZXRob2SRKg=="),
            new ProtocolTestData(
                name: "StreamInvocationWithNoHeadersAndEnumArg",
                message: new StreamInvocationMessage("xyz", "method", new object[] { TestEnum.One }),
                binary: "lQSAo3h5eqZtZXRob2SRo09uZQ=="),
            new ProtocolTestData(
                name: "StreamInvocationWithNoHeadersAndIntAndStringArgs",
                message: new StreamInvocationMessage("xyz", "method", new object[] { 42, "string" }),
                binary: "lQSAo3h5eqZtZXRob2SSKqZzdHJpbmc="),
            new ProtocolTestData(
                name: "StreamInvocationWithNoHeadersAndIntStringAndCustomObjectArgs",
                message: new StreamInvocationMessage("xyz", "method", new object[] { 42, "string", new CustomObject() }),
                binary: "lQSAo3h5eqZtZXRob2STKqZzdHJpbmeGqlN0cmluZ1Byb3CoU2lnbmFsUiGqRG91YmxlUHJvcMtAGSH7VELPEqdJbnRQcm9wKqxEYXRlVGltZVByb3DW/1jsHICoTnVsbFByb3DAq0J5dGVBcnJQcm9wxAMBAgM="),
            new ProtocolTestData(
                name: "StreamInvocationWithNoHeadersAndCustomObjectArrayArg",
                message: new StreamInvocationMessage("xyz", "method", new object[] { new CustomObject(), new CustomObject() }),
                binary: "lQSAo3h5eqZtZXRob2SShqpTdHJpbmdQcm9wqFNpZ25hbFIhqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqsRGF0ZVRpbWVQcm9w1v9Y7ByAqE51bGxQcm9wwKtCeXRlQXJyUHJvcMQDAQIDhqpTdHJpbmdQcm9wqFNpZ25hbFIhqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqsRGF0ZVRpbWVQcm9w1v9Y7ByAqE51bGxQcm9wwKtCeXRlQXJyUHJvcMQDAQID"),
            new ProtocolTestData(
                name: "StreamInvocationWithHeadersAndCustomObjectArrayArg",
                message: AddHeaders(TestHeaders, new StreamInvocationMessage("xyz", "method", new object[] { new CustomObject(), new CustomObject() })),
                binary: "lQSDo0Zvb6NCYXKyS2V5V2l0aApOZXcNCkxpbmVzq1N0aWxsIFdvcmtzsVZhbHVlV2l0aE5ld0xpbmVzsEFsc28KV29ya3MNCkZpbmWjeHl6pm1ldGhvZJKGqlN0cmluZ1Byb3CoU2lnbmFsUiGqRG91YmxlUHJvcMtAGSH7VELPEqdJbnRQcm9wKqxEYXRlVGltZVByb3DW/1jsHICoTnVsbFByb3DAq0J5dGVBcnJQcm9wxAMBAgOGqlN0cmluZ1Byb3CoU2lnbmFsUiGqRG91YmxlUHJvcMtAGSH7VELPEqdJbnRQcm9wKqxEYXRlVGltZVByb3DW/1jsHICoTnVsbFByb3DAq0J5dGVBcnJQcm9wxAMBAgM="),

            // CancelInvocation Messages
            new ProtocolTestData(
                name: "CancelInvocationWithNoHeaders",
                message: new CancelInvocationMessage("xyz"),
                binary: "kwWAo3h5eg=="),
            new ProtocolTestData(
                name: "CancelInvocationWithHeaders",
                message: AddHeaders(TestHeaders, new CancelInvocationMessage("xyz")),
                binary: "kwWDo0Zvb6NCYXKyS2V5V2l0aApOZXcNCkxpbmVzq1N0aWxsIFdvcmtzsVZhbHVlV2l0aE5ld0xpbmVzsEFsc28KV29ya3MNCkZpbmWjeHl6"),

            // StreamComplete Messages
            new ProtocolTestData(
                name: "StreamComplete",
                message: new StreamCompleteMessage("xyz"),
                binary: "kwijeHl6wA=="),
            new ProtocolTestData(
                name: "StreamCompleteWithError",
                message: new StreamCompleteMessage("xyz", "zoinks"),
                binary: "kwijeHl6pnpvaW5rcw=="),

            // Ping Messages
            new ProtocolTestData(
                name: "Ping",
                message: PingMessage.Instance,
                binary: "kQY="),

            // StreamData Messages
            new ProtocolTestData(
                name: "StreamData",
                message: new StreamDataMessage("xyz", new CustomObject()),
                binary: "kwmjeHl6hqpTdHJpbmdQcm9wqFNpZ25hbFIhqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqsRGF0ZVRpbWVQcm9w1v9Y7ByAqE51bGxQcm9wwKtCeXRlQXJyUHJvcMQDAQID"),
        }.ToDictionary(t => t.Name);

        [Theory]
        [MemberData(nameof(TestDataNames))]
        public void ParseMessages(string testDataName)
        {
            var testData = TestData[testDataName];

            // Verify that the input binary string decodes to the expected MsgPack primitives
            var bytes = Convert.FromBase64String(testData.Binary);

            // Parse the input fully now.
            bytes = Frame(bytes);
            var data = new ReadOnlySequence<byte>(bytes);
            Assert.True(_hubProtocol.TryParseMessage(ref data, new TestBinder(testData.Message), out var message));

            Assert.NotNull(message);
            Assert.Equal(testData.Message, message, TestHubMessageEqualityComparer.Instance);
        }

        [Fact]
        public void ParseMessageWithExtraData()
        {
            var expectedMessage = new InvocationMessage("xyz", "method", Array.Empty<object>());

            // Verify that the input binary string decodes to the expected MsgPack primitives
            var bytes = new byte[] { ArrayBytes(8),
                1,
                0x80,
                StringBytes(3), (byte)'x', (byte)'y', (byte)'z',
                StringBytes(6), (byte)'m', (byte)'e', (byte)'t', (byte)'h', (byte)'o', (byte)'d',
                ArrayBytes(0),
                0xc3,
                StringBytes(2), (byte)'e', (byte)'x' };

            // Parse the input fully now.
            bytes = Frame(bytes);
            var data = new ReadOnlySequence<byte>(bytes);
            Assert.True(_hubProtocol.TryParseMessage(ref data, new TestBinder(expectedMessage), out var message));

            Assert.NotNull(message);
            Assert.Equal(expectedMessage, message, TestHubMessageEqualityComparer.Instance);
        }

        [Theory]
        [MemberData(nameof(TestDataNames))]
        public void WriteMessages(string testDataName)
        {
            var testData = TestData[testDataName];

            var bytes = Write(testData.Message);

            // Unframe the message to check the binary encoding
            var byteSpan = new ReadOnlySequence<byte>(bytes);
            Assert.True(BinaryMessageParser.TryParseMessage(ref byteSpan, out var unframed));

            // Check the baseline binary encoding, use Assert.True in order to configure the error message
            var actual = Convert.ToBase64String(unframed.ToArray());
            Assert.True(string.Equals(actual, testData.Binary, StringComparison.Ordinal), $"Binary encoding changed from{Environment.NewLine} [{testData.Binary}]{Environment.NewLine} to{Environment.NewLine} [{actual}]{Environment.NewLine}Please verify the MsgPack output and update the baseline");
        }

        [Fact]
        public void WriteAndParseDateTimeConvertsToUTC()
        {
            var dateTime = new DateTime(2018, 4, 9);
            var writer = MemoryBufferWriter.Get();

            try
            {
                _hubProtocol.WriteMessage(CompletionMessage.WithResult("xyz", dateTime), writer);
                var bytes = new ReadOnlySequence<byte>(writer.ToArray());
                _hubProtocol.TryParseMessage(ref bytes, new TestBinder(typeof(DateTime)), out var hubMessage);

                var completionMessage = Assert.IsType<CompletionMessage>(hubMessage);

                var resultDateTime = (DateTime)completionMessage.Result;
                // The messagepack Timestamp format specifies that time is stored as seconds since 1970-01-01 UTC
                // so the library has no choice but to store the time as UTC
                // https://github.com/msgpack/msgpack/blob/master/spec.md#timestamp-extension-type
                Assert.Equal(dateTime.ToUniversalTime(), resultDateTime);
            }
            finally
            {
                MemoryBufferWriter.Return(writer);
            }
        }

        [Fact]
        public void WriteAndParseDateTimeOffset()
        {
            var dateTimeOffset = new DateTimeOffset(new DateTime(2018, 4, 9), TimeSpan.FromHours(10));
            var writer = MemoryBufferWriter.Get();

            try
            {
                _hubProtocol.WriteMessage(CompletionMessage.WithResult("xyz", dateTimeOffset), writer);
                var bytes = new ReadOnlySequence<byte>(writer.ToArray());
                _hubProtocol.TryParseMessage(ref bytes, new TestBinder(typeof(DateTimeOffset)), out var hubMessage);

                var completionMessage = Assert.IsType<CompletionMessage>(hubMessage);

                var resultDateTimeOffset = (DateTimeOffset)completionMessage.Result;
                Assert.Equal(dateTimeOffset, resultDateTimeOffset);
            }
            finally
            {
                MemoryBufferWriter.Return(writer);
            }
        }

        public static IDictionary<string, InvalidMessageData> InvalidPayloads => new[]
        {
            // Message Type
            new InvalidMessageData("MessageTypeString", new byte[] { 0x91, 0xa3, (byte)'f', (byte)'o', (byte)'o' }, "Reading 'messageType' as Int32 failed."),

            // Headers
            new InvalidMessageData("HeadersNotAMap", new byte[] { 0x92, 1, 0xa3, (byte)'f', (byte)'o', (byte)'o' }, "Reading map length for 'headers' failed."),
            new InvalidMessageData("HeaderKeyInt", new byte[] { 0x92, 1, 0x82, 0x2a, 0xa3, (byte)'f', (byte)'o', (byte)'o' }, "Reading 'headers[0].Key' as String failed."),
            new InvalidMessageData("HeaderValueInt", new byte[] { 0x92, 1, 0x82, 0xa3, (byte)'f', (byte)'o', (byte)'o', 42 }, "Reading 'headers[0].Value' as String failed."),
            new InvalidMessageData("HeaderKeyArray", new byte[] { 0x92, 1, 0x84, 0xa3, (byte)'f', (byte)'o', (byte)'o', 0xa3, (byte)'f', (byte)'o', (byte)'o', 0x90, 0xa3, (byte)'f', (byte)'o', (byte)'o' }, "Reading 'headers[1].Key' as String failed."),
            new InvalidMessageData("HeaderValueArray", new byte[] { 0x92, 1, 0x84, 0xa3, (byte)'f', (byte)'o', (byte)'o', 0xa3, (byte)'f', (byte)'o', (byte)'o', 0xa3, (byte)'f', (byte)'o', (byte)'o', 0x90 }, "Reading 'headers[1].Value' as String failed."),

            // InvocationMessage
            new InvalidMessageData("InvocationMissingId", new byte[] { 0x92, 1, 0x80 }, "Reading 'invocationId' as String failed."),
            new InvalidMessageData("InvocationIdBoolean", new byte[] { 0x91, 1, 0x80, 0xc2 }, "Reading 'invocationId' as String failed."),
            new InvalidMessageData("InvocationTargetMissing", new byte[] { 0x93, 1, 0x80, 0xa3, (byte)'a', (byte)'b', (byte)'c' }, "Reading 'target' as String failed."),
            new InvalidMessageData("InvocationTargetInt", new byte[] { 0x94, 1, 0x80, 0xa3, (byte)'a', (byte)'b', (byte)'c', 42 }, "Reading 'target' as String failed."),

            // StreamInvocationMessage
            new InvalidMessageData("StreamInvocationMissingId", new byte[] { 0x92, 4, 0x80 }, "Reading 'invocationId' as String failed."),
            new InvalidMessageData("StreamInvocationIdBoolean", new byte[] { 0x93, 4, 0x80, 0xc2 }, "Reading 'invocationId' as String failed."),
            new InvalidMessageData("StreamInvocationTargetMissing", new byte[] { 0x93, 4, 0x80, 0xa3, (byte)'a', (byte)'b', (byte)'c' }, "Reading 'target' as String failed."),
            new InvalidMessageData("StreamInvocationTargetInt", new byte[] { 0x94, 4, 0x80, 0xa3, (byte)'a', (byte)'b', (byte)'c', 42 }, "Reading 'target' as String failed."),

            // StreamItemMessage
            new InvalidMessageData("StreamItemMissingId", new byte[] { 0x92, 2, 0x80 }, "Reading 'invocationId' as String failed."),
            new InvalidMessageData("StreamItemInvocationIdBoolean", new byte[] { 0x93, 2, 0x80, 0xc2 }, "Reading 'invocationId' as String failed."),
            new InvalidMessageData("StreamItemMissing", new byte[] { 0x93, 2, 0x80, 0xa3, (byte)'x', (byte)'y', (byte)'z' }, "Deserializing object of the `String` type for 'item' failed."),
            new InvalidMessageData("StreamItemTypeMismatch", new byte[] { 0x94, 2, 0x80, 0xa3, (byte)'x', (byte)'y', (byte)'z', 42 }, "Deserializing object of the `String` type for 'item' failed."),

            // CompletionMessage
            new InvalidMessageData("CompletionMissingId", new byte[] { 0x92, 3, 0x80 }, "Reading 'invocationId' as String failed."),
            new InvalidMessageData("CompletionIdBoolean", new byte[] { 0x93, 3, 0x80, 0xc2 }, "Reading 'invocationId' as String failed."),
            new InvalidMessageData("CompletionResultKindString", new byte[] { 0x94, 3, 0x80, 0xa3, (byte)'x', (byte)'y', (byte)'z', 0xa3, (byte)'x', (byte)'y', (byte)'z' }, "Reading 'resultKind' as Int32 failed."),
            new InvalidMessageData("CompletionResultKindOutOfRange", new byte[] { 0x94, 3, 0x80, 0xa3, (byte)'x', (byte)'y', (byte)'z', 42 }, "Invalid invocation result kind."),
            new InvalidMessageData("CompletionErrorMissing", new byte[] { 0x94, 3, 0x80, 0xa3, (byte)'x', (byte)'y', (byte)'z', 0x01 }, "Reading 'error' as String failed."),
            new InvalidMessageData("CompletionErrorInt", new byte[] { 0x95, 3, 0x80, 0xa3, (byte)'x', (byte)'y', (byte)'z', 0x01, 42 }, "Reading 'error' as String failed."),
            new InvalidMessageData("CompletionResultMissing", new byte[] { 0x94, 3, 0x80, 0xa3, (byte)'x', (byte)'y', (byte)'z', 0x03 }, "Deserializing object of the `String` type for 'argument' failed."),
            new InvalidMessageData("CompletionResultTypeMismatch", new byte[] { 0x95, 3, 0x80, 0xa3, (byte)'x', (byte)'y', (byte)'z', 0x03, 42 }, "Deserializing object of the `String` type for 'argument' failed."),
        }.ToDictionary(t => t.Name);

        public static IEnumerable<object[]> InvalidPayloadNames => InvalidPayloads.Keys.Select(name => new object[] { name });

        [Theory]
        [MemberData(nameof(InvalidPayloadNames))]
        public void ParserThrowsForInvalidMessages(string invalidPayloadName)
        {
            var testData = InvalidPayloads[invalidPayloadName];

            var buffer = Frame(testData.Encoded);
            var binder = new TestBinder(new[] { typeof(string) }, typeof(string));
            var data = new ReadOnlySequence<byte>(buffer);
            var exception = Assert.Throws<InvalidDataException>(() => _hubProtocol.TryParseMessage(ref data, binder, out _));

            Assert.Equal(testData.ErrorMessage, exception.Message);
        }

        public static IDictionary<string, InvalidMessageData> ArgumentBindingErrors => new[]
        {
            // InvocationMessage
            new InvalidMessageData("InvocationArgumentArrayMissing", new byte[] { 0x94, 1, 0x80, 0xa3, (byte)'a', (byte)'b', (byte)'c', 0xa3, (byte)'x', (byte)'y', (byte)'z' }, "Reading array length for 'arguments' failed."),
            new InvalidMessageData("InvocationArgumentArrayNotAnArray", new byte[] { 0x95, 1, 0x80, 0xa3, (byte)'a', (byte)'b', (byte)'c', 0xa3, (byte)'x', (byte)'y', (byte)'z', 42 }, "Reading array length for 'arguments' failed."),
            new InvalidMessageData("InvocationArgumentArraySizeMismatchEmpty", new byte[] { 0x95, 1, 0x80, 0xa3, (byte)'a', (byte)'b', (byte)'c', 0xa3, (byte)'x', (byte)'y', (byte)'z', 0x90 }, "Invocation provides 0 argument(s) but target expects 1."),
            new InvalidMessageData("InvocationArgumentArraySizeMismatchTooLarge", new byte[] { 0x95, 1, 0x80, 0xa3, (byte)'a', (byte)'b', (byte)'c', 0xa3, (byte)'x', (byte)'y', (byte)'z', 0x92, 0xa1, (byte)'a', 0xa1, (byte)'b' }, "Invocation provides 2 argument(s) but target expects 1."),
            new InvalidMessageData("InvocationArgumentTypeMismatch", new byte[] { 0x95, 1, 0x80, 0xa3, (byte)'a', (byte)'b', (byte)'c', 0xa3, (byte)'x', (byte)'y', (byte)'z', 0x91, 42 }, "Error binding arguments. Make sure that the types of the provided values match the types of the hub method being invoked."),

            // StreamInvocationMessage
            new InvalidMessageData("StreamInvocationArgumentArrayMissing", new byte[] { 0x94, 4, 0x80, 0xa3, (byte)'a', (byte)'b', (byte)'c', 0xa3, (byte)'x', (byte)'y', (byte)'z' }, "Reading array length for 'arguments' failed."), // array is missing
            new InvalidMessageData("StreamInvocationArgumentArrayNotAnArray", new byte[] { 0x95, 4, 0x80, 0xa3, (byte)'a', (byte)'b', (byte)'c', 0xa3, (byte)'x', (byte)'y', (byte)'z', 42 }, "Reading array length for 'arguments' failed."), // arguments isn't an array
            new InvalidMessageData("StreamInvocationArgumentArraySizeMismatchEmpty", new byte[] { 0x95, 4, 0x80, 0xa3, (byte)'a', (byte)'b', (byte)'c', 0xa3, (byte)'x', (byte)'y', (byte)'z', 0x90 }, "Invocation provides 0 argument(s) but target expects 1."), // array is missing elements
            new InvalidMessageData("StreamInvocationArgumentArraySizeMismatchTooLarge", new byte[] { 0x95, 4, 0x80, 0xa3, (byte)'a', (byte)'b', (byte)'c', 0xa3, (byte)'x', (byte)'y', (byte)'z', 0x92, 0xa1, (byte)'a', 0xa1, (byte)'b' }, "Invocation provides 2 argument(s) but target expects 1."), // argument count does not match binder argument count
            new InvalidMessageData("StreamInvocationArgumentTypeMismatch", new byte[] { 0x95, 4, 0x80, 0xa3, (byte)'a', (byte)'b', (byte)'c', 0xa3, (byte)'x', (byte)'y', (byte)'z', 0x91, 42 }, "Error binding arguments. Make sure that the types of the provided values match the types of the hub method being invoked."), // argument type mismatch
        }.ToDictionary(t => t.Name);

        public static IEnumerable<object[]> ArgumentBindingErrorNames => ArgumentBindingErrors.Keys.Select(name => new object[] { name });

        [Theory]
        [MemberData(nameof(ArgumentBindingErrorNames))]
        public void GettingArgumentsThrowsIfBindingFailed(string argumentBindingErrorName)
        {
            var testData = ArgumentBindingErrors[argumentBindingErrorName];

            var buffer = Frame(testData.Encoded);
            var binder = new TestBinder(new[] { typeof(string) }, typeof(string));
            var data = new ReadOnlySequence<byte>(buffer);
            _hubProtocol.TryParseMessage(ref data, binder, out var message);
            var bindingFailure = Assert.IsType<InvocationBindingFailureMessage>(message);
            Assert.Equal(testData.ErrorMessage, bindingFailure.BindingFailure.SourceException.Message);
        }

        [Theory]
        [InlineData(new byte[] { 0x05, 0x01 })]
        public void ParserDoesNotConsumePartialData(byte[] payload)
        {
            var binder = new TestBinder(new[] { typeof(string) }, typeof(string));
            var data = new ReadOnlySequence<byte>(payload);
            var result = _hubProtocol.TryParseMessage(ref data, binder, out var message);
            Assert.Null(message);
        }

        [Fact]
        public void SerializerCanSerializeTypesWithNoDefaultCtor()
        {
            var result = Write(CompletionMessage.WithResult("0", new List<int> { 42 }.AsReadOnly()));
            AssertMessages(new byte[] { ArrayBytes(5), 3, 0x80, StringBytes(1), (byte)'0', 0x03, ArrayBytes(1), 42 }, result);
        }

        private byte ArrayBytes(int size)
        {
            Debug.Assert(size < 16, "Test code doesn't support array sizes greater than 15");

            return (byte)(0x90 | size);
        }

        private byte StringBytes(int size)
        {
            Debug.Assert(size < 16, "Test code doesn't support string sizes greater than 15");

            return (byte)(0xa0 | size);
        }

        private static void AssertMessages(byte[] expectedOutput, ReadOnlyMemory<byte> bytes)
        {
            var data = new ReadOnlySequence<byte>(bytes);
            Assert.True(BinaryMessageParser.TryParseMessage(ref data, out var message));
            Assert.Equal(expectedOutput, message.ToArray());
        }

        private static byte[] Frame(byte[] input)
        {
            var stream = MemoryBufferWriter.Get();
            try
            {
                BinaryMessageFormatter.WriteLengthPrefix(input.Length, stream);
                stream.Write(input);
                return stream.ToArray();
            }
            finally
            {
                MemoryBufferWriter.Return(stream);
            }
        }

        private static byte[] Write(HubMessage message)
        {
            var writer = MemoryBufferWriter.Get();
            try
            {
                _hubProtocol.WriteMessage(message, writer);
                return writer.ToArray();
            }
            finally
            {
                MemoryBufferWriter.Return(writer);
            }
        }

        public class InvalidMessageData
        {
            public string Name { get; private set; }
            public byte[] Encoded { get; private set; }
            public string ErrorMessage { get; private set; }

            public InvalidMessageData(string name, byte[] encoded, string errorMessage)
            {
                Name = name;
                Encoded = encoded;
                ErrorMessage = errorMessage;
            }

            public override string ToString() => Name;
        }

        public class ProtocolTestData
        {
            public string Name { get; }
            public string Binary { get; }
            public HubMessage Message { get; }

            public ProtocolTestData(string name, HubMessage message, string binary)
            {
                Name = name;
                Message = message;
                Binary = binary;
            }

            public override string ToString() => Name;
        }
    }
}
