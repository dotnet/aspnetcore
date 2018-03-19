// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.SignalR.Internal.Formatters;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using MsgPack;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol
{
    using static HubMessageHelpers;
    using static MessagePackHelpers;

    public class MessagePackHubProtocolTests
    {
        private static readonly IDictionary<string, string> TestHeaders = new Dictionary<string, string>
        {
            { "Foo", "Bar" },
            { "KeyWith\nNew\r\nLines", "Still Works" },
            { "ValueWithNewLines", "Also\nWorks\r\nFine" },
        };

        private static MessagePackObject TestHeadersSerialized = Map(
            ("Foo", "Bar"),
            ("KeyWith\nNew\r\nLines", "Still Works"),
            ("ValueWithNewLines", "Also\nWorks\r\nFine"));

        private static readonly MessagePackHubProtocol _hubProtocol
            = new MessagePackHubProtocol();

        private static MessagePackObject CustomObjectSerialized = Map(
            ("ByteArrProp", new MessagePackObject(new byte[] { 1, 2, 3 }, isBinary: true)),
            ("DateTimeProp", new MessagePackObject(Timestamp.FromDateTime(new DateTime(2017, 4, 11)))),
            ("DoubleProp", 6.2831853071),
            ("IntProp", 42),
            ("NullProp", MessagePackObject.Nil),
            ("StringProp", "SignalR!"));

        // Test Data for Parse/WriteMessages:
        // * Name: A string name that is used when reporting the test (it's the ToString value for ProtocolTestData)
        // * Message: The HubMessage that is either expected (in Parse) or used as input (in Write)
        // * Encoded: Raw MessagePackObject values (using the MessagePackHelpers static "Arr" and "Map" helpers) describing the message
        // * Binary: Base64-encoded binary "baseline" to sanity-check MsgPack-Cli behavior
        //
        // The Encoded value is used as input to "Parse" and as the expected output that is verified in "Write". So if our encoding changes,
        // those values will change and the Assert will give you a useful error telling you how the MsgPack structure itself changed (rather than just
        // a bunch of random bytes). However, we want to be sure MsgPack-Cli doesn't change behavior, so we also verify that the binary encoding
        // matches our expectation by comparing against a base64-string.
        //
        // If you change MsgPack encoding, you should update the 'encoded' values for these items, and then re-run the test. You'll get a failure which will
        // provide a new Base64 binary string to replace in the 'binary' value. Use a tool like https://sugendran.github.io/msgpack-visualizer/ to verify
        // that the MsgPack is correct and then just replace the Base64 value.

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
                message: new InvocationMessage(invocationId: "xyz", target: "method", argumentBindingException: null),
                encoded: Array(HubProtocolConstants.InvocationMessageType, Map(), "xyz", "method", Array()),
                binary: "lQGAo3h5eqZtZXRob2SQ"),
            new ProtocolTestData(
                name: "InvocationWithNoHeadersNoIdAndNoArgs",
                message: new InvocationMessage(target: "method", argumentBindingException: null),
                encoded: Array(HubProtocolConstants.InvocationMessageType, Map(), MessagePackObject.Nil, "method", Array()),
                binary: "lQGAwKZtZXRob2SQ"),
            new ProtocolTestData(
                name: "InvocationWithNoHeadersNoIdAndSingleNullArg",
                message: new InvocationMessage(target: "method", argumentBindingException: null, new object[] { null }),
                encoded: Array(HubProtocolConstants.InvocationMessageType, Map(), MessagePackObject.Nil, "method", Array(MessagePackObject.Nil)),
                binary: "lQGAwKZtZXRob2SRwA=="),
            new ProtocolTestData(
                name: "InvocationWithNoHeadersNoIdAndSingleIntArg",
                message: new InvocationMessage(target: "method", argumentBindingException: null, 42),
                encoded: Array(HubProtocolConstants.InvocationMessageType, Map(), MessagePackObject.Nil, "method", Array(42)),
                binary: "lQGAwKZtZXRob2SRKg=="),
            new ProtocolTestData(
                name: "InvocationWithNoHeadersNoIdIntAndStringArgs",
                message: new InvocationMessage(target: "method", argumentBindingException: null, 42, "string"),
                encoded: Array(HubProtocolConstants.InvocationMessageType, Map(), MessagePackObject.Nil, "method", Array(42, "string")),
                binary: "lQGAwKZtZXRob2SSKqZzdHJpbmc="),
            new ProtocolTestData(
                name: "InvocationWithNoHeadersNoIdAndCustomObjectArg",
                message: new InvocationMessage(target: "method", argumentBindingException: null, 42, "string", new CustomObject()),
                encoded: Array(HubProtocolConstants.InvocationMessageType, Map(), MessagePackObject.Nil, "method", Array(42, "string", CustomObjectSerialized)),
                binary: "lQGAwKZtZXRob2STKqZzdHJpbmeGq0J5dGVBcnJQcm9wxAMBAgOsRGF0ZVRpbWVQcm9w1v9Y7ByAqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqoTnVsbFByb3DAqlN0cmluZ1Byb3CoU2lnbmFsUiE="),
            new ProtocolTestData(
                name: "InvocationWithNoHeadersNoIdAndArrayOfCustomObjectArgs",
                message: new InvocationMessage(target: "method", argumentBindingException: null, new[] { new CustomObject(), new CustomObject() }),
                encoded: Array(HubProtocolConstants.InvocationMessageType, Map(), MessagePackObject.Nil, "method", Array(CustomObjectSerialized, CustomObjectSerialized)),
                binary: "lQGAwKZtZXRob2SShqtCeXRlQXJyUHJvcMQDAQIDrERhdGVUaW1lUHJvcNb/WOwcgKpEb3VibGVQcm9wy0AZIftUQs8Sp0ludFByb3AqqE51bGxQcm9wwKpTdHJpbmdQcm9wqFNpZ25hbFIhhqtCeXRlQXJyUHJvcMQDAQIDrERhdGVUaW1lUHJvcNb/WOwcgKpEb3VibGVQcm9wy0AZIftUQs8Sp0ludFByb3AqqE51bGxQcm9wwKpTdHJpbmdQcm9wqFNpZ25hbFIh"),
            new ProtocolTestData(
                name: "InvocationWithHeadersNoIdAndArrayOfCustomObjectArgs",
                message: AddHeaders(TestHeaders, new InvocationMessage(target: "method", argumentBindingException: null, new[] { new CustomObject(), new CustomObject() })),
                encoded: Array(HubProtocolConstants.InvocationMessageType, TestHeadersSerialized, MessagePackObject.Nil, "method", Array(CustomObjectSerialized, CustomObjectSerialized)),
                binary: "lQGDo0Zvb6NCYXKyS2V5V2l0aApOZXcNCkxpbmVzq1N0aWxsIFdvcmtzsVZhbHVlV2l0aE5ld0xpbmVzsEFsc28KV29ya3MNCkZpbmXApm1ldGhvZJKGq0J5dGVBcnJQcm9wxAMBAgOsRGF0ZVRpbWVQcm9w1v9Y7ByAqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqoTnVsbFByb3DAqlN0cmluZ1Byb3CoU2lnbmFsUiGGq0J5dGVBcnJQcm9wxAMBAgOsRGF0ZVRpbWVQcm9w1v9Y7ByAqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqoTnVsbFByb3DAqlN0cmluZ1Byb3CoU2lnbmFsUiE="),

            // StreamItem Messages
            new ProtocolTestData(
                name: "StreamItemWithNoHeadersAndNullItem",
                message: new StreamItemMessage(invocationId: "xyz", item: null),
                encoded: Array(HubProtocolConstants.StreamItemMessageType, Map(), "xyz", MessagePackObject.Nil),
                binary: "lAKAo3h5esA="),
            new ProtocolTestData(
                name: "StreamItemWithNoHeadersAndIntItem",
                message: new StreamItemMessage(invocationId: "xyz", item: 42),
                encoded: Array(HubProtocolConstants.StreamItemMessageType, Map(), "xyz", 42),
                binary: "lAKAo3h5eio="),
            new ProtocolTestData(
                name: "StreamItemWithNoHeadersAndFloatItem",
                message: new StreamItemMessage(invocationId: "xyz", item: 42.0f),
                encoded: Array(HubProtocolConstants.StreamItemMessageType, Map(), "xyz", 42.0f),
                binary: "lAKAo3h5espCKAAA"),
            new ProtocolTestData(
                name: "StreamItemWithNoHeadersAndStringItem",
                message: new StreamItemMessage(invocationId: "xyz", item: "string"),
                encoded: Array(HubProtocolConstants.StreamItemMessageType, Map(), "xyz", "string"),
                binary: "lAKAo3h5eqZzdHJpbmc="),
            new ProtocolTestData(
                name: "StreamItemWithNoHeadersAndBoolItem",
                message: new StreamItemMessage(invocationId: "xyz", item: true),
                encoded: Array(HubProtocolConstants.StreamItemMessageType, Map(), "xyz", true),
                binary: "lAKAo3h5esM="),
            new ProtocolTestData(
                name: "StreamItemWithNoHeadersAndCustomObjectItem",
                message: new StreamItemMessage(invocationId: "xyz", item: new CustomObject()),
                encoded: Array(HubProtocolConstants.StreamItemMessageType, Map(), "xyz", CustomObjectSerialized),
                binary: "lAKAo3h5eoarQnl0ZUFyclByb3DEAwECA6xEYXRlVGltZVByb3DW/1jsHICqRG91YmxlUHJvcMtAGSH7VELPEqdJbnRQcm9wKqhOdWxsUHJvcMCqU3RyaW5nUHJvcKhTaWduYWxSIQ=="),
            new ProtocolTestData(
                name: "StreamItemWithNoHeadersAndCustomObjectArrayItem",
                message: new StreamItemMessage(invocationId: "xyz", item: new[] { new CustomObject(), new CustomObject() }),
                encoded: Array(HubProtocolConstants.StreamItemMessageType, Map(), "xyz", Array(CustomObjectSerialized, CustomObjectSerialized)),
                binary: "lAKAo3h5epKGq0J5dGVBcnJQcm9wxAMBAgOsRGF0ZVRpbWVQcm9w1v9Y7ByAqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqoTnVsbFByb3DAqlN0cmluZ1Byb3CoU2lnbmFsUiGGq0J5dGVBcnJQcm9wxAMBAgOsRGF0ZVRpbWVQcm9w1v9Y7ByAqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqoTnVsbFByb3DAqlN0cmluZ1Byb3CoU2lnbmFsUiE="),
            new ProtocolTestData(
                name: "StreamItemWithHeadersAndCustomObjectArrayItem",
                message: AddHeaders(TestHeaders, new StreamItemMessage(invocationId: "xyz", item: new[] { new CustomObject(), new CustomObject() })),
                encoded: Array(HubProtocolConstants.StreamItemMessageType, TestHeadersSerialized, "xyz", Array(CustomObjectSerialized, CustomObjectSerialized)),
                binary: "lAKDo0Zvb6NCYXKyS2V5V2l0aApOZXcNCkxpbmVzq1N0aWxsIFdvcmtzsVZhbHVlV2l0aE5ld0xpbmVzsEFsc28KV29ya3MNCkZpbmWjeHl6koarQnl0ZUFyclByb3DEAwECA6xEYXRlVGltZVByb3DW/1jsHICqRG91YmxlUHJvcMtAGSH7VELPEqdJbnRQcm9wKqhOdWxsUHJvcMCqU3RyaW5nUHJvcKhTaWduYWxSIYarQnl0ZUFyclByb3DEAwECA6xEYXRlVGltZVByb3DW/1jsHICqRG91YmxlUHJvcMtAGSH7VELPEqdJbnRQcm9wKqhOdWxsUHJvcMCqU3RyaW5nUHJvcKhTaWduYWxSIQ=="),

            // Completion Messages
            new ProtocolTestData(
                name: "CompletionWithNoHeadersAndError",
                message: CompletionMessage.WithError(invocationId: "xyz", error: "Error not found!"),
                encoded: Array(HubProtocolConstants.CompletionMessageType, Map(), "xyz", 1, "Error not found!"),
                binary: "lQOAo3h5egGwRXJyb3Igbm90IGZvdW5kIQ=="),
            new ProtocolTestData(
                name: "CompletionWithHeadersAndError",
                message: AddHeaders(TestHeaders, CompletionMessage.WithError(invocationId: "xyz", error: "Error not found!")),
                encoded: Array(HubProtocolConstants.CompletionMessageType, TestHeadersSerialized, "xyz", 1, "Error not found!"),
                binary: "lQODo0Zvb6NCYXKyS2V5V2l0aApOZXcNCkxpbmVzq1N0aWxsIFdvcmtzsVZhbHVlV2l0aE5ld0xpbmVzsEFsc28KV29ya3MNCkZpbmWjeHl6AbBFcnJvciBub3QgZm91bmQh"),
            new ProtocolTestData(
                name: "CompletionWithNoHeadersAndNoResult",
                message: CompletionMessage.Empty(invocationId: "xyz"),
                encoded: Array(HubProtocolConstants.CompletionMessageType, Map(), "xyz", 2),
                binary: "lAOAo3h5egI="),
            new ProtocolTestData(
                name: "CompletionWithHeadersAndNoResult",
                message: AddHeaders(TestHeaders, CompletionMessage.Empty(invocationId: "xyz")),
                encoded: Array(HubProtocolConstants.CompletionMessageType, TestHeadersSerialized, "xyz", 2),
                binary: "lAODo0Zvb6NCYXKyS2V5V2l0aApOZXcNCkxpbmVzq1N0aWxsIFdvcmtzsVZhbHVlV2l0aE5ld0xpbmVzsEFsc28KV29ya3MNCkZpbmWjeHl6Ag=="),
            new ProtocolTestData(
                name: "CompletionWithNoHeadersAndNullResult",
                message: CompletionMessage.WithResult(invocationId: "xyz", payload: null),
                encoded: Array(HubProtocolConstants.CompletionMessageType, Map(), "xyz", 3, MessagePackObject.Nil),
                binary: "lQOAo3h5egPA"),
            new ProtocolTestData(
                name: "CompletionWithNoHeadersAndIntResult",
                message: CompletionMessage.WithResult(invocationId: "xyz", payload: 42),
                encoded: Array(HubProtocolConstants.CompletionMessageType, Map(), "xyz", 3, 42),
                binary: "lQOAo3h5egMq"),
            new ProtocolTestData(
                name: "CompletionWithNoHeadersAndFloatResult",
                message: CompletionMessage.WithResult(invocationId: "xyz", payload: 42.0f),
                encoded: Array(HubProtocolConstants.CompletionMessageType, Map(), "xyz", 3, 42.0f),
                binary: "lQOAo3h5egPKQigAAA=="),
            new ProtocolTestData(
                name: "CompletionWithNoHeadersAndStringResult",
                message: CompletionMessage.WithResult(invocationId: "xyz", payload: "string"),
                encoded: Array(HubProtocolConstants.CompletionMessageType, Map(), "xyz", 3, "string"),
                binary: "lQOAo3h5egOmc3RyaW5n"),
            new ProtocolTestData(
                name: "CompletionWithNoHeadersAndBooleanResult",
                message: CompletionMessage.WithResult(invocationId: "xyz", payload: true),
                encoded: Array(HubProtocolConstants.CompletionMessageType, Map(), "xyz", 3, true),
                binary: "lQOAo3h5egPD"),
            new ProtocolTestData(
                name: "CompletionWithNoHeadersAndCustomObjectResult",
                message: CompletionMessage.WithResult(invocationId: "xyz", payload: new CustomObject()),
                encoded: Array(HubProtocolConstants.CompletionMessageType, Map(), "xyz", 3, CustomObjectSerialized),
                binary: "lQOAo3h5egOGq0J5dGVBcnJQcm9wxAMBAgOsRGF0ZVRpbWVQcm9w1v9Y7ByAqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqoTnVsbFByb3DAqlN0cmluZ1Byb3CoU2lnbmFsUiE="),
            new ProtocolTestData(
                name: "CompletionWithNoHeadersAndCustomObjectArrayResult",
                message: CompletionMessage.WithResult(invocationId: "xyz", payload: new[] { new CustomObject(), new CustomObject() }),
                encoded: Array(HubProtocolConstants.CompletionMessageType, Map(), "xyz", 3, Array(CustomObjectSerialized, CustomObjectSerialized)),
                binary: "lQOAo3h5egOShqtCeXRlQXJyUHJvcMQDAQIDrERhdGVUaW1lUHJvcNb/WOwcgKpEb3VibGVQcm9wy0AZIftUQs8Sp0ludFByb3AqqE51bGxQcm9wwKpTdHJpbmdQcm9wqFNpZ25hbFIhhqtCeXRlQXJyUHJvcMQDAQIDrERhdGVUaW1lUHJvcNb/WOwcgKpEb3VibGVQcm9wy0AZIftUQs8Sp0ludFByb3AqqE51bGxQcm9wwKpTdHJpbmdQcm9wqFNpZ25hbFIh"),
            new ProtocolTestData(
                name: "CompletionWithHeadersAndCustomObjectArrayResult",
                message: AddHeaders(TestHeaders, CompletionMessage.WithResult(invocationId: "xyz", payload: new[] { new CustomObject(), new CustomObject() })),
                encoded: Array(HubProtocolConstants.CompletionMessageType, TestHeadersSerialized, "xyz", 3, Array(CustomObjectSerialized, CustomObjectSerialized)),
                binary: "lQODo0Zvb6NCYXKyS2V5V2l0aApOZXcNCkxpbmVzq1N0aWxsIFdvcmtzsVZhbHVlV2l0aE5ld0xpbmVzsEFsc28KV29ya3MNCkZpbmWjeHl6A5KGq0J5dGVBcnJQcm9wxAMBAgOsRGF0ZVRpbWVQcm9w1v9Y7ByAqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqoTnVsbFByb3DAqlN0cmluZ1Byb3CoU2lnbmFsUiGGq0J5dGVBcnJQcm9wxAMBAgOsRGF0ZVRpbWVQcm9w1v9Y7ByAqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqoTnVsbFByb3DAqlN0cmluZ1Byb3CoU2lnbmFsUiE="),

            // StreamInvocation Messages
            new ProtocolTestData(
                name: "StreamInvocationWithNoHeadersAndNoArgs",
                message: new StreamInvocationMessage(invocationId: "xyz", target: "method", argumentBindingException: null),
                encoded: Array(HubProtocolConstants.StreamInvocationMessageType, Map(), "xyz", "method", Array()),
                binary: "lQSAo3h5eqZtZXRob2SQ"),
            new ProtocolTestData(
                name: "StreamInvocationWithNoHeadersAndNullArg",
                message: new StreamInvocationMessage(invocationId: "xyz", target: "method", argumentBindingException: null, new object[] { null }),
                encoded: Array(HubProtocolConstants.StreamInvocationMessageType, Map(), "xyz", "method", Array(MessagePackObject.Nil)),
                binary: "lQSAo3h5eqZtZXRob2SRwA=="),
            new ProtocolTestData(
                name: "StreamInvocationWithNoHeadersAndIntArg",
                message: new StreamInvocationMessage(invocationId: "xyz", target: "method", argumentBindingException: null, 42),
                encoded: Array(HubProtocolConstants.StreamInvocationMessageType, Map(), "xyz", "method", Array(42)),
                binary: "lQSAo3h5eqZtZXRob2SRKg=="),
            new ProtocolTestData(
                name: "StreamInvocationWithNoHeadersAndIntAndStringArgs",
                message: new StreamInvocationMessage(invocationId: "xyz", target: "method", argumentBindingException: null, 42, "string"),
                encoded: Array(HubProtocolConstants.StreamInvocationMessageType, Map(), "xyz", "method", Array(42, "string")),
                binary: "lQSAo3h5eqZtZXRob2SSKqZzdHJpbmc="),
            new ProtocolTestData(
                name: "StreamInvocationWithNoHeadersAndIntStringAndCustomObjectArgs",
                message: new StreamInvocationMessage(invocationId: "xyz", target: "method", argumentBindingException: null, 42, "string", new CustomObject()),
                encoded: Array(HubProtocolConstants.StreamInvocationMessageType, Map(), "xyz", "method", Array(42, "string", CustomObjectSerialized)),
                binary: "lQSAo3h5eqZtZXRob2STKqZzdHJpbmeGq0J5dGVBcnJQcm9wxAMBAgOsRGF0ZVRpbWVQcm9w1v9Y7ByAqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqoTnVsbFByb3DAqlN0cmluZ1Byb3CoU2lnbmFsUiE="),
            new ProtocolTestData(
                name: "StreamInvocationWithNoHeadersAndCustomObjectArrayArg",
                message: new StreamInvocationMessage(invocationId: "xyz", target: "method", argumentBindingException: null, new[] { new CustomObject(), new CustomObject() }),
                encoded: Array(HubProtocolConstants.StreamInvocationMessageType, Map(), "xyz", "method", Array(CustomObjectSerialized, CustomObjectSerialized)),
                binary: "lQSAo3h5eqZtZXRob2SShqtCeXRlQXJyUHJvcMQDAQIDrERhdGVUaW1lUHJvcNb/WOwcgKpEb3VibGVQcm9wy0AZIftUQs8Sp0ludFByb3AqqE51bGxQcm9wwKpTdHJpbmdQcm9wqFNpZ25hbFIhhqtCeXRlQXJyUHJvcMQDAQIDrERhdGVUaW1lUHJvcNb/WOwcgKpEb3VibGVQcm9wy0AZIftUQs8Sp0ludFByb3AqqE51bGxQcm9wwKpTdHJpbmdQcm9wqFNpZ25hbFIh"),
            new ProtocolTestData(
                name: "StreamInvocationWithHeadersAndCustomObjectArrayArg",
                message: AddHeaders(TestHeaders, new StreamInvocationMessage(invocationId: "xyz", target: "method", argumentBindingException: null, new[] { new CustomObject(), new CustomObject() })),
                encoded: Array(HubProtocolConstants.StreamInvocationMessageType, TestHeadersSerialized, "xyz", "method", Array(CustomObjectSerialized, CustomObjectSerialized)),
                binary: "lQSDo0Zvb6NCYXKyS2V5V2l0aApOZXcNCkxpbmVzq1N0aWxsIFdvcmtzsVZhbHVlV2l0aE5ld0xpbmVzsEFsc28KV29ya3MNCkZpbmWjeHl6pm1ldGhvZJKGq0J5dGVBcnJQcm9wxAMBAgOsRGF0ZVRpbWVQcm9w1v9Y7ByAqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqoTnVsbFByb3DAqlN0cmluZ1Byb3CoU2lnbmFsUiGGq0J5dGVBcnJQcm9wxAMBAgOsRGF0ZVRpbWVQcm9w1v9Y7ByAqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqoTnVsbFByb3DAqlN0cmluZ1Byb3CoU2lnbmFsUiE="),

            // CancelInvocation Messages
            new ProtocolTestData(
                name: "CancelInvocationWithNoHeaders",
                message: new CancelInvocationMessage(invocationId: "xyz"),
                encoded: Array(HubProtocolConstants.CancelInvocationMessageType, Map(), "xyz"),
                binary: "kwWAo3h5eg=="),
            new ProtocolTestData(
                name: "CancelInvocationWithHeaders",
                message: AddHeaders(TestHeaders, new CancelInvocationMessage(invocationId: "xyz")),
                encoded: Array(HubProtocolConstants.CancelInvocationMessageType, TestHeadersSerialized, "xyz"),
                binary: "kwWDo0Zvb6NCYXKyS2V5V2l0aApOZXcNCkxpbmVzq1N0aWxsIFdvcmtzsVZhbHVlV2l0aE5ld0xpbmVzsEFsc28KV29ya3MNCkZpbmWjeHl6"),

            // Ping Messages
            new ProtocolTestData(
                name: "Ping",
                message: PingMessage.Instance,
                encoded: Array(HubProtocolConstants.PingMessageType),
                binary: "kQY="),
        }.ToDictionary(t => t.Name);

        [Theory]
        [MemberData(nameof(TestDataNames))]
        public void ParseMessages(string testDataName)
        {
            var testData = TestData[testDataName];

            // Verify that the input binary string decodes to the expected MsgPack primitives
            var bytes = Convert.FromBase64String(testData.Binary);
            var obj = Unpack(bytes);
            Assert.Equal(testData.Encoded, obj);

            // Parse the input fully now.
            bytes = Frame(bytes);
            var protocol = new MessagePackHubProtocol();
            var messages = new List<HubMessage>();
            Assert.True(protocol.TryParseMessages(bytes, new TestBinder(testData.Message), messages));

            Assert.Single(messages);
            Assert.Equal(testData.Message, messages[0], TestHubMessageEqualityComparer.Instance);
        }

        [Theory]
        [MemberData(nameof(TestDataNames))]
        public void WriteMessages(string testDataName)
        {
            var testData = TestData[testDataName];

            var bytes = Write(testData.Message);
            AssertMessages(testData.Encoded, bytes);

            // Unframe the message to check the binary encoding
            ReadOnlyMemory<byte> byteSpan = bytes;
            Assert.True(BinaryMessageParser.TryParseMessage(ref byteSpan, out var unframed));

            // Check the baseline binary encoding, use Assert.True in order to configure the error message
            var actual = Convert.ToBase64String(unframed.ToArray());
            Assert.True(string.Equals(actual, testData.Binary, StringComparison.Ordinal), $"Binary encoding changed from{Environment.NewLine} [{testData.Binary}]{Environment.NewLine} to{Environment.NewLine} [{actual}]{Environment.NewLine}Please verify the MsgPack output and update the baseline");
        }

        public static IEnumerable<object[]> InvalidPayloads => new[]
        {
            // Message Type
            new object[] { new InvalidMessageData("MessageTypeString", Array("foo"), "Reading 'messageType' as Int32 failed.") },
            new object[] { new InvalidMessageData("MessageTypeOutOfRange", Array(10), "Invalid message type: 10.") },

            // Headers
            new object[] { new InvalidMessageData("HeadersNotAMap", Array(HubProtocolConstants.InvocationMessageType, "foo"), "Reading map length for 'headers' failed.") },
            new object[] { new InvalidMessageData("HeaderKeyInt", Array(HubProtocolConstants.InvocationMessageType, Map((42, "foo"))), "Reading 'headers[0].Key' as String failed.") },
            new object[] { new InvalidMessageData("HeaderValueInt", Array(HubProtocolConstants.InvocationMessageType, Map(("foo", 42))), "Reading 'headers[0].Value' as String failed.") },
            new object[] { new InvalidMessageData("HeaderKeyArray", Array(HubProtocolConstants.InvocationMessageType, Map(("biz", "boz"), (Array(), "foo"))), "Reading 'headers[1].Key' as String failed.") },
            new object[] { new InvalidMessageData("HeaderValueArray", Array(HubProtocolConstants.InvocationMessageType, Map(("biz", "boz"), ("foo", Array()))), "Reading 'headers[1].Value' as String failed.") },

            // InvocationMessage
            new object[] { new InvalidMessageData("InvocationMissingId", Array(HubProtocolConstants.InvocationMessageType, Map()), "Reading 'invocationId' as String failed.") },
            new object[] { new InvalidMessageData("InvocationIdBoolean", Array(HubProtocolConstants.InvocationMessageType, Map(), false), "Reading 'invocationId' as String failed.") },
            new object[] { new InvalidMessageData("InvocationTargetMissing", Array(HubProtocolConstants.InvocationMessageType, Map(), "abc"), "Reading 'target' as String failed.") },
            new object[] { new InvalidMessageData("InvocationTargetInt", Array(HubProtocolConstants.InvocationMessageType, Map(), "abc", 42), "Reading 'target' as String failed.") },

            // StreamInvocationMessage
            new object[] { new InvalidMessageData("StreamInvocationMissingId", Array(HubProtocolConstants.StreamInvocationMessageType, Map()), "Reading 'invocationId' as String failed.") },
            new object[] { new InvalidMessageData("StreamInvocationIdBoolean", Array(HubProtocolConstants.StreamInvocationMessageType, Map(), false), "Reading 'invocationId' as String failed.") },
            new object[] { new InvalidMessageData("StreamInvocationTargetMissing", Array(HubProtocolConstants.StreamInvocationMessageType, Map(), "abc"), "Reading 'target' as String failed.") },
            new object[] { new InvalidMessageData("StreamInvocationTargetInt", Array(HubProtocolConstants.StreamInvocationMessageType, Map(), "abc", 42), "Reading 'target' as String failed.") },

            // StreamItemMessage
            new object[] { new InvalidMessageData("StreamItemMissingId", Array(HubProtocolConstants.StreamItemMessageType, Map()), "Reading 'invocationId' as String failed.") },
            new object[] { new InvalidMessageData("StreamItemInvocationIdBoolean", Array(HubProtocolConstants.StreamItemMessageType, Map(), false), "Reading 'invocationId' as String failed.") },
            new object[] { new InvalidMessageData("StreamItemMissing", Array(HubProtocolConstants.StreamItemMessageType, Map(), "xyz"), "Deserializing object of the `String` type for 'item' failed.") },
            new object[] { new InvalidMessageData("StreamItemTypeMismatch", Array(HubProtocolConstants.StreamItemMessageType, Map(), "xyz", 42), "Deserializing object of the `String` type for 'item' failed.") },

            // CompletionMessage
            new object[] { new InvalidMessageData("CompletionMissingId", Array(HubProtocolConstants.CompletionMessageType, Map()), "Reading 'invocationId' as String failed.") },
            new object[] { new InvalidMessageData("CompletionIdBoolean", Array(HubProtocolConstants.CompletionMessageType, Map(), false), "Reading 'invocationId' as String failed.") },
            new object[] { new InvalidMessageData("CompletionResultKindString", Array(HubProtocolConstants.CompletionMessageType, Map(), "xyz", "abc"), "Reading 'resultKind' as Int32 failed.") },
            new object[] { new InvalidMessageData("CompletionResultKindOutOfRange", Array(HubProtocolConstants.CompletionMessageType, Map(), "xyz", 42), "Invalid invocation result kind.") },
            new object[] { new InvalidMessageData("CompletionErrorMissing", Array(HubProtocolConstants.CompletionMessageType, Map(), "xyz", 1), "Reading 'error' as String failed.") },
            new object[] { new InvalidMessageData("CompletionErrorInt", Array(HubProtocolConstants.CompletionMessageType, Map(), "xyz", 1, 42), "Reading 'error' as String failed.") },
            new object[] { new InvalidMessageData("CompletionResultMissing", Array(HubProtocolConstants.CompletionMessageType, Map(), "xyz", 3), "Deserializing object of the `String` type for 'argument' failed.") },
            new object[] { new InvalidMessageData("CompletionResultTypeMismatch", Array(HubProtocolConstants.CompletionMessageType, Map(), "xyz", 3, 42), "Deserializing object of the `String` type for 'argument' failed.") },
        };

        [Theory]
        [MemberData(nameof(InvalidPayloads))]
        public void ParserThrowsForInvalidMessages(InvalidMessageData testData)
        {
            var buffer = Frame(Pack(testData.Encoded));
            var binder = new TestBinder(new[] { typeof(string) }, typeof(string));
            var messages = new List<HubMessage>();
            var exception = Assert.Throws<FormatException>(() => _hubProtocol.TryParseMessages(buffer, binder, messages));

            Assert.Equal(testData.ErrorMessage, exception.Message);
        }

        public static IEnumerable<object[]> ArgumentBindingErrors => new[]
        {
            // InvocationMessage
            new object[] {new InvalidMessageData("InvocationArgumentArrayMissing", Array(HubProtocolConstants.InvocationMessageType, Map(), "abc", "xyz"), "Reading array length for 'arguments' failed.") },
            new object[] {new InvalidMessageData("InvocationArgumentArrayNotAnArray", Array(HubProtocolConstants.InvocationMessageType, Map(), "abc", "xyz", 42), "Reading array length for 'arguments' failed.") },
            new object[] {new InvalidMessageData("InvocationArgumentArraySizeMismatchEmpty", Array(HubProtocolConstants.InvocationMessageType, Map(), "abc", "xyz", Array()), "Invocation provides 0 argument(s) but target expects 1.") },
            new object[] {new InvalidMessageData("InvocationArgumentArraySizeMismatchTooLarge", Array(HubProtocolConstants.InvocationMessageType, Map(), "abc", "xyz", Array("a", "b")), "Invocation provides 2 argument(s) but target expects 1.") },
            new object[] {new InvalidMessageData("InvocationArgumentTypeMismatch", Array(HubProtocolConstants.InvocationMessageType, Map(), "abc", "xyz", Array(42)), "Error binding arguments. Make sure that the types of the provided values match the types of the hub method being invoked.") },

            // StreamInvocationMessage
            new object[] {new InvalidMessageData("StreamInvocationArgumentArrayMissing", Array(HubProtocolConstants.StreamInvocationMessageType, Map(), "abc", "xyz"), "Reading array length for 'arguments' failed.") }, // array is missing
            new object[] {new InvalidMessageData("StreamInvocationArgumentArrayNotAnArray", Array(HubProtocolConstants.StreamInvocationMessageType, Map(), "abc", "xyz", 42), "Reading array length for 'arguments' failed.") }, // arguments isn't an array
            new object[] {new InvalidMessageData("StreamInvocationArgumentArraySizeMismatchEmpty", Array(HubProtocolConstants.StreamInvocationMessageType, Map(), "abc", "xyz", Array()), "Invocation provides 0 argument(s) but target expects 1.") }, // array is missing elements
            new object[] {new InvalidMessageData("StreamInvocationArgumentArraySizeMismatchTooLarge", Array(HubProtocolConstants.StreamInvocationMessageType, Map(), "abc", "xyz", Array("a", "b")), "Invocation provides 2 argument(s) but target expects 1.") }, // argument count does not match binder argument count
            new object[] {new InvalidMessageData("StreamInvocationArgumentTypeMismatch", Array(HubProtocolConstants.StreamInvocationMessageType, Map(), "abc", "xyz", Array(42)), "Error binding arguments. Make sure that the types of the provided values match the types of the hub method being invoked.") }, // argument type mismatch
        };

        [Theory]
        [MemberData(nameof(ArgumentBindingErrors))]
        public void GettingArgumentsThrowsIfBindingFailed(InvalidMessageData testData)
        {
            var buffer = Frame(Pack(testData.Encoded));
            var binder = new TestBinder(new[] { typeof(string) }, typeof(string));
            var messages = new List<HubMessage>();
            _hubProtocol.TryParseMessages(buffer, binder, messages);
            var exception = Assert.Throws<FormatException>(() => ((HubMethodInvocationMessage)messages[0]).Arguments);

            Assert.Equal(testData.ErrorMessage, exception.Message);
        }

        [Theory]
        [InlineData(new object[] { new byte[] { 0x05, 0x01 }, 0 })]
        public void ParserDoesNotConsumePartialData(byte[] payload, int expectedMessagesCount)
        {
            var binder = new TestBinder(new[] { typeof(string) }, typeof(string));
            var messages = new List<HubMessage>();
            var result = _hubProtocol.TryParseMessages(payload, binder, messages);
            Assert.True(result || messages.Count == 0);
            Assert.Equal(expectedMessagesCount, messages.Count);
        }

        [Fact]
        public void SerializerCanSerializeTypesWithNoDefaultCtor()
        {
            var result = Write(CompletionMessage.WithResult("0", new List<int> { 42 }.AsReadOnly()));
            AssertMessages(Array(HubProtocolConstants.CompletionMessageType, Map(), "0", 3, Array(42)), result);
        }

        private static void AssertMessages(MessagePackObject expectedOutput, ReadOnlyMemory<byte> bytes)
        {
            Assert.True(BinaryMessageParser.TryParseMessage(ref bytes, out var message));
            var obj = Unpack(message.ToArray());
            Assert.Equal(expectedOutput, obj);
        }

        private static byte[] Frame(byte[] input)
        {
            using (var stream = new MemoryStream())
            {
                BinaryMessageFormatter.WriteLengthPrefix(input.Length, stream);
                stream.Write(input, 0, input.Length);
                return stream.ToArray();
            }
        }

        private static MessagePackObject Unpack(byte[] input)
        {
            using (var stream = new MemoryStream(input))
            {
                using (var unpacker = Unpacker.Create(stream))
                {
                    Assert.True(unpacker.ReadObject(out var obj));
                    return obj;
                }
            }
        }

        private static byte[] Pack(MessagePackObject input)
        {
            var options = new PackingOptions()
            {
                StringEncoding = Encoding.UTF8
            };

            using (var stream = new MemoryStream())
            {
                using (var packer = Packer.Create(stream))
                {
                    input.PackToMessage(packer, options);
                    packer.Flush();
                }
                stream.Flush();
                return stream.ToArray();
            }
        }

        private static byte[] Write(HubMessage message)
        {
            var protocol = new MessagePackHubProtocol();
            using (var stream = new MemoryStream())
            {
                protocol.WriteMessage(message, stream);
                stream.Flush();
                return stream.ToArray();
            }
        }

        public class InvalidMessageData
        {
            public string Name { get; private set; }
            public MessagePackObject Encoded { get; private set; }
            public string ErrorMessage { get; private set; }

            public InvalidMessageData(string name, MessagePackObject encoded, string errorMessage)
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
            public MessagePackObject Encoded { get; }
            public HubMessage Message { get; }

            public ProtocolTestData(string name, HubMessage message, MessagePackObject encoded, string binary)
            {
                Name = name;
                Message = message;
                Encoded = encoded;
                Binary = binary;
            }

            public override string ToString() => Name;
        }
    }
}
