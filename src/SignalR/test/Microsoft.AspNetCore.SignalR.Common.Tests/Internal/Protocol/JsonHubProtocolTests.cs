// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol
{
    using static HubMessageHelpers;

    public class JsonHubProtocolTests
    {
        private static readonly IDictionary<string, string> TestHeaders = new Dictionary<string, string>
        {
            { "Foo", "Bar" },
            { "KeyWith\nNew\r\nLines", "Still Works" },
            { "ValueWithNewLines", "Also\nWorks\r\nFine" },
        };

        // It's cleaner to do this as a prefix and use concatenation rather than string interpolation because JSON is already filled with '{'s.
        private static readonly string SerializedHeaders = "\"headers\":{\"Foo\":\"Bar\",\"KeyWith\\nNew\\r\\nLines\":\"Still Works\",\"ValueWithNewLines\":\"Also\\nWorks\\r\\nFine\"}";

        public static IDictionary<string, JsonProtocolTestData> ProtocolTestData => new[]
        {
            new JsonProtocolTestData("InvocationMessage_HasInvocationId", new InvocationMessage("123", "Target", new object[] { 1, "Foo", 2.0f }), true, NullValueHandling.Ignore, "{\"type\":1,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[1,\"Foo\",2.0]}"),
            new JsonProtocolTestData("InvocationMessage_HasFloatArgument", new InvocationMessage(null, "Target", new object[] { 1, "Foo", 2.0f }), true, NullValueHandling.Ignore, "{\"type\":1,\"target\":\"Target\",\"arguments\":[1,\"Foo\",2.0]}"),
            new JsonProtocolTestData("InvocationMessage_HasBoolArgument", new InvocationMessage(null, "Target", new object[] { true }), true, NullValueHandling.Ignore, "{\"type\":1,\"target\":\"Target\",\"arguments\":[true]}"),
            new JsonProtocolTestData("InvocationMessage_HasNullArgument", new InvocationMessage(null, "Target", new object[] { null }), true, NullValueHandling.Ignore, "{\"type\":1,\"target\":\"Target\",\"arguments\":[null]}"),
            new JsonProtocolTestData("InvocationMessage_HasCustomArgumentWithNoCamelCase", new InvocationMessage(null, "Target", new object[] { new CustomObject() }), false, NullValueHandling.Ignore, "{\"type\":1,\"target\":\"Target\",\"arguments\":[{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"ByteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("InvocationMessage_HasCustomArgumentWithNullValueIgnore", new InvocationMessage(null, "Target", new object[] { new CustomObject() }), true, NullValueHandling.Ignore, "{\"type\":1,\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"byteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("InvocationMessage_HasCustomArgumentWithNullValueIgnoreAndNoCamelCase", new InvocationMessage(null, "Target", new object[] { new CustomObject() }), false, NullValueHandling.Include, "{\"type\":1,\"target\":\"Target\",\"arguments\":[{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"NullProp\":null,\"ByteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("InvocationMessage_HasCustomArgumentWithNullValueInclude", new InvocationMessage(null, "Target", new object[] { new CustomObject() }), true, NullValueHandling.Include, "{\"type\":1,\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("InvocationMessage_HasStreamPlaceholder", new InvocationMessage(null, "Target", new object[] { new StreamPlaceholder("__test_id__")}), true, NullValueHandling.Ignore, "{\"type\":1,\"target\":\"Target\",\"arguments\":[{\"streamId\":\"__test_id__\"}]}"),
            new JsonProtocolTestData("InvocationMessage_HasHeaders", AddHeaders(TestHeaders, new InvocationMessage("123", "Target", new object[] { 1, "Foo", 2.0f })), true, NullValueHandling.Ignore, "{\"type\":1," + SerializedHeaders + ",\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[1,\"Foo\",2.0]}"),
            new JsonProtocolTestData("InvocationMessage_StringIsoDateArgument", new InvocationMessage("Method", new object[] { "2016-05-10T13:51:20+12:34" }), true, NullValueHandling.Ignore, "{\"type\":1,\"target\":\"Method\",\"arguments\":[\"2016-05-10T13:51:20+12:34\"]}"),
            new JsonProtocolTestData("InvocationMessage_DateTimeOffsetArgument", new InvocationMessage("Method", new object[] { DateTimeOffset.Parse("2016-05-10T13:51:20+12:34") }), true, NullValueHandling.Ignore, "{\"type\":1,\"target\":\"Method\",\"arguments\":[\"2016-05-10T13:51:20+12:34\"]}"),

            new JsonProtocolTestData("StreamItemMessage_HasIntegerItem", new StreamItemMessage("123", 1), true, NullValueHandling.Ignore, "{\"type\":2,\"invocationId\":\"123\",\"item\":1}"),
            new JsonProtocolTestData("StreamItemMessage_HasStringItem", new StreamItemMessage("123", "Foo"), true, NullValueHandling.Ignore, "{\"type\":2,\"invocationId\":\"123\",\"item\":\"Foo\"}"),
            new JsonProtocolTestData("StreamItemMessage_HasFloatItem", new StreamItemMessage("123", 2.0f), true, NullValueHandling.Ignore, "{\"type\":2,\"invocationId\":\"123\",\"item\":2.0}"),
            new JsonProtocolTestData("StreamItemMessage_HasBoolItem", new StreamItemMessage("123", true), true, NullValueHandling.Ignore, "{\"type\":2,\"invocationId\":\"123\",\"item\":true}"),
            new JsonProtocolTestData("StreamItemMessage_HasNullItem", new StreamItemMessage("123", null), true, NullValueHandling.Ignore, "{\"type\":2,\"invocationId\":\"123\",\"item\":null}"),
            new JsonProtocolTestData("StreamItemMessage_HasCustomItemWithNoCamelCase", new StreamItemMessage("123", new CustomObject()), false, NullValueHandling.Ignore, "{\"type\":2,\"invocationId\":\"123\",\"item\":{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"ByteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("StreamItemMessage_HasCustomItemWithNullValueIgnore", new StreamItemMessage("123", new CustomObject()), true, NullValueHandling.Ignore, "{\"type\":2,\"invocationId\":\"123\",\"item\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"byteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("StreamItemMessage_HasCustomItemWithNullValueIgnoreAndNoCamelCase", new StreamItemMessage("123", new CustomObject()), false, NullValueHandling.Include, "{\"type\":2,\"invocationId\":\"123\",\"item\":{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"NullProp\":null,\"ByteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("StreamItemMessage_HasCustomItemWithNullValueInclude", new StreamItemMessage("123", new CustomObject()), true, NullValueHandling.Include, "{\"type\":2,\"invocationId\":\"123\",\"item\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("StreamItemMessage_HasHeaders", AddHeaders(TestHeaders, new StreamItemMessage("123", new CustomObject())), true, NullValueHandling.Include, "{\"type\":2," + SerializedHeaders + ",\"invocationId\":\"123\",\"item\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}}"),

            new JsonProtocolTestData("CompletionMessage_HasIntegerResult", CompletionMessage.WithResult("123", 1), true, NullValueHandling.Ignore, "{\"type\":3,\"invocationId\":\"123\",\"result\":1}"),
            new JsonProtocolTestData("CompletionMessage_HasStringResult", CompletionMessage.WithResult("123", "Foo"), true, NullValueHandling.Ignore, "{\"type\":3,\"invocationId\":\"123\",\"result\":\"Foo\"}"),
            new JsonProtocolTestData("CompletionMessage_HasFloatResult", CompletionMessage.WithResult("123", 2.0f), true, NullValueHandling.Ignore, "{\"type\":3,\"invocationId\":\"123\",\"result\":2.0}"),
            new JsonProtocolTestData("CompletionMessage_HasBoolResult", CompletionMessage.WithResult("123", true), true, NullValueHandling.Ignore, "{\"type\":3,\"invocationId\":\"123\",\"result\":true}"),
            new JsonProtocolTestData("CompletionMessage_HasNullResult", CompletionMessage.WithResult("123", null), true, NullValueHandling.Ignore, "{\"type\":3,\"invocationId\":\"123\",\"result\":null}"),
            new JsonProtocolTestData("CompletionMessage_HasCustomResultWithNoCamelCase", CompletionMessage.WithResult("123", new CustomObject()), false, NullValueHandling.Ignore, "{\"type\":3,\"invocationId\":\"123\",\"result\":{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"ByteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("CompletionMessage_HasCustomResultWithNullValueIgnore", CompletionMessage.WithResult("123", new CustomObject()), true, NullValueHandling.Ignore, "{\"type\":3,\"invocationId\":\"123\",\"result\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"byteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("CompletionMessage_HasCustomResultWithNullValueIncludeAndNoCamelCase", CompletionMessage.WithResult("123", new CustomObject()), false, NullValueHandling.Include, "{\"type\":3,\"invocationId\":\"123\",\"result\":{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"NullProp\":null,\"ByteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("CompletionMessage_HasCustomResultWithNullValueInclude", CompletionMessage.WithResult("123", new CustomObject()), true, NullValueHandling.Include, "{\"type\":3,\"invocationId\":\"123\",\"result\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("CompletionMessage_HasTestHeadersAndCustomItemResult", AddHeaders(TestHeaders, CompletionMessage.WithResult("123", new CustomObject())), true, NullValueHandling.Include, "{\"type\":3," + SerializedHeaders + ",\"invocationId\":\"123\",\"result\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("CompletionMessage_HasError", CompletionMessage.WithError("123", "Whoops!"), false, NullValueHandling.Ignore, "{\"type\":3,\"invocationId\":\"123\",\"error\":\"Whoops!\"}"),
            new JsonProtocolTestData("CompletionMessage_HasErrorAndHeaders", AddHeaders(TestHeaders, CompletionMessage.WithError("123", "Whoops!")), false, NullValueHandling.Ignore, "{\"type\":3," + SerializedHeaders + ",\"invocationId\":\"123\",\"error\":\"Whoops!\"}"),
            new JsonProtocolTestData("CompletionMessage_HasErrorAndCamelCase", CompletionMessage.Empty("123"), true, NullValueHandling.Ignore, "{\"type\":3,\"invocationId\":\"123\"}"),
            new JsonProtocolTestData("CompletionMessage_HasErrorAndHeadersAndCamelCase", AddHeaders(TestHeaders, CompletionMessage.Empty("123")), true, NullValueHandling.Ignore, "{\"type\":3," + SerializedHeaders + ",\"invocationId\":\"123\"}"),

            new JsonProtocolTestData("StreamInvocationMessage_HasInvocationId", new StreamInvocationMessage("123", "Target", new object[] { 1, "Foo", 2.0f }), true, NullValueHandling.Ignore, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[1,\"Foo\",2.0]}"),
            new JsonProtocolTestData("StreamInvocationMessage_HasFloatArgument", new StreamInvocationMessage("123", "Target", new object[] { 1, "Foo", 2.0f }), true, NullValueHandling.Ignore, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[1,\"Foo\",2.0]}"),
            new JsonProtocolTestData("StreamInvocationMessage_HasBoolArgument", new StreamInvocationMessage("123", "Target", new object[] { true }), true, NullValueHandling.Ignore, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[true]}"),
            new JsonProtocolTestData("StreamInvocationMessage_HasNullArgument", new StreamInvocationMessage("123", "Target", new object[] { null }), true, NullValueHandling.Ignore, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[null]}"),
            new JsonProtocolTestData("StreamInvocationMessage_HasCustomArgumentWithNoCamelCase", new StreamInvocationMessage("123", "Target", new object[] { new CustomObject() }), false, NullValueHandling.Ignore, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"ByteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("StreamInvocationMessage_HasCustomArgumentWithNullValueIgnore", new StreamInvocationMessage("123", "Target", new object[] { new CustomObject() }), true, NullValueHandling.Ignore, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"byteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("StreamInvocationMessage_HasCustomArgumentWithNullValueIgnoreAndNoCamelCase", new StreamInvocationMessage("123", "Target", new object[] { new CustomObject() }), false, NullValueHandling.Include, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"NullProp\":null,\"ByteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("StreamInvocationMessage_HasCustomArgumentWithNullValueInclude", new StreamInvocationMessage("123", "Target", new object[] { new CustomObject() }), true, NullValueHandling.Include, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("StreamInvocationMessage_HasHeaders", AddHeaders(TestHeaders, new StreamInvocationMessage("123", "Target", new object[] { new CustomObject() })), true, NullValueHandling.Include, "{\"type\":4," + SerializedHeaders + ",\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}]}"),

            new JsonProtocolTestData("CancelInvocationMessage_HasInvocationId", new CancelInvocationMessage("123"), true, NullValueHandling.Ignore, "{\"type\":5,\"invocationId\":\"123\"}"),
            new JsonProtocolTestData("CancelInvocationMessage_HasHeaders", AddHeaders(TestHeaders, new CancelInvocationMessage("123")), true, NullValueHandling.Ignore, "{\"type\":5," + SerializedHeaders + ",\"invocationId\":\"123\"}"),

            new JsonProtocolTestData("PingMessage", PingMessage.Instance, true, NullValueHandling.Ignore, "{\"type\":6}"),

            new JsonProtocolTestData("CloseMessage", CloseMessage.Empty, false, NullValueHandling.Ignore, "{\"type\":7}"),
            new JsonProtocolTestData("CloseMessage_HasError", new CloseMessage("Error!"), false, NullValueHandling.Ignore, "{\"type\":7,\"error\":\"Error!\"}"),
            new JsonProtocolTestData("CloseMessage_HasErrorWithCamelCase", new CloseMessage("Error!"), true, NullValueHandling.Ignore, "{\"type\":7,\"error\":\"Error!\"}"),
            new JsonProtocolTestData("CloseMessage_HasErrorEmptyString", new CloseMessage(""), false, NullValueHandling.Ignore, "{\"type\":7,\"error\":\"\"}"),

            new JsonProtocolTestData("StreamCompleteMessage", new StreamCompleteMessage("123"), true, NullValueHandling.Ignore, "{\"type\":8,\"streamId\":\"123\"}"),
            new JsonProtocolTestData("StreamCompleteMessageWithError", new StreamCompleteMessage("123", "zoinks"), true, NullValueHandling.Ignore, "{\"type\":8,\"streamId\":\"123\",\"error\":\"zoinks\"}"),

        }.ToDictionary(t => t.Name);

        public static IEnumerable<object[]> ProtocolTestDataNames => ProtocolTestData.Keys.Select(name => new object[] { name });

        public static IDictionary<string, JsonProtocolTestData> OutOfOrderJsonTestData => new[]
        {
            new JsonProtocolTestData("InvocationMessage_StringIsoDateArgumentFirst", new InvocationMessage("Method", new object[] { "2016-05-10T13:51:20+12:34" }), false, NullValueHandling.Ignore, "{ \"arguments\": [\"2016-05-10T13:51:20+12:34\"], \"type\":1, \"target\": \"Method\" }"),
            new JsonProtocolTestData("InvocationMessage_DateTimeOffsetArgumentFirst", new InvocationMessage("Method", new object[] { DateTimeOffset.Parse("2016-05-10T13:51:20+12:34") }), false, NullValueHandling.Ignore, "{ \"arguments\": [\"2016-05-10T13:51:20+12:34\"], \"type\":1, \"target\": \"Method\" }"),
            new JsonProtocolTestData("InvocationMessage_IntegerArrayArgumentFirst", new InvocationMessage("Method", new object[] { 1, 2 }), false, NullValueHandling.Ignore, "{ \"arguments\": [1,2], \"type\":1, \"target\": \"Method\" }"),
            new JsonProtocolTestData("StreamInvocationMessage_IntegerArrayArgumentFirst", new StreamInvocationMessage("3", "Method", new object[] { 1, 2 }), false, NullValueHandling.Ignore, "{ \"type\":4, \"arguments\": [1,2], \"target\": \"Method\", \"invocationId\": \"3\" }"),
            new JsonProtocolTestData("CompletionMessage_ResultFirst", new CompletionMessage("15", null, 10, hasResult: true), false, NullValueHandling.Ignore, "{ \"type\":3, \"result\": 10, \"invocationId\": \"15\" }"),
            new JsonProtocolTestData("StreamItemMessage_ItemFirst", new StreamItemMessage("1a", "foo"), false, NullValueHandling.Ignore, "{ \"item\": \"foo\", \"invocationId\": \"1a\", \"type\":2 }")

        }.ToDictionary(t => t.Name);

        public static IEnumerable<object[]> OutOfOrderJsonTestDataNames => OutOfOrderJsonTestData.Keys.Select(name => new object[] { name });

        [Theory]
        [MemberData(nameof(ProtocolTestDataNames))]
        public void WriteMessage(string protocolTestDataName)
        {
            var testData = ProtocolTestData[protocolTestDataName];

            var expectedOutput = Frame(testData.Json);

            var protocolOptions = new NewtonsoftJsonHubProtocolOptions
            {
                PayloadSerializerSettings = new JsonSerializerSettings()
                {
                    NullValueHandling = testData.NullValueHandling,
                    ContractResolver = testData.CamelCase ? new CamelCasePropertyNamesContractResolver() : new DefaultContractResolver()
                }
            };

            var protocol = new NewtonsoftJsonHubProtocol(Options.Create(protocolOptions));

            var writer = MemoryBufferWriter.Get();
            try
            {
                protocol.WriteMessage(testData.Message, writer);
                var json = Encoding.UTF8.GetString(writer.ToArray());

                Assert.Equal(expectedOutput, json);
            }
            finally
            {
                MemoryBufferWriter.Return(writer);
            }
        }

        [Theory]
        [MemberData(nameof(ProtocolTestDataNames))]
        public void ParseMessage(string protocolTestDataName)
        {
            var testData = ProtocolTestData[protocolTestDataName];

            var input = Frame(testData.Json);

            var protocolOptions = new NewtonsoftJsonHubProtocolOptions
            {
                PayloadSerializerSettings = new JsonSerializerSettings
                {
                    NullValueHandling = testData.NullValueHandling,
                    ContractResolver = testData.CamelCase ? new CamelCasePropertyNamesContractResolver() : new DefaultContractResolver()
                }
            };

            var binder = new TestBinder(testData.Message);
            var protocol = new NewtonsoftJsonHubProtocol(Options.Create(protocolOptions));
            var data = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(input));
            protocol.TryParseMessage(ref data, binder, out var message);

            Assert.Equal(testData.Message, message, TestHubMessageEqualityComparer.Instance);
        }

        [Theory]
        [InlineData("", "Unexpected end when reading JSON.")]
        [InlineData("null", "Unexpected JSON Token Type 'Null'. Expected a JSON Object.")]
        [InlineData("42", "Unexpected JSON Token Type 'Integer'. Expected a JSON Object.")]
        [InlineData("'foo'", "Unexpected JSON Token Type 'String'. Expected a JSON Object.")]
        [InlineData("[42]", "Unexpected JSON Token Type 'Array'. Expected a JSON Object.")]
        [InlineData("{}", "Missing required property 'type'.")]

        [InlineData("{'type':1,'headers':{\"Foo\": 42},'target':'test',arguments:[]}", "Expected header 'Foo' to be of type String.")]
        [InlineData("{'type':1,'headers':{\"Foo\": true},'target':'test',arguments:[]}", "Expected header 'Foo' to be of type String.")]
        [InlineData("{'type':1,'headers':{\"Foo\": null},'target':'test',arguments:[]}", "Expected header 'Foo' to be of type String.")]
        [InlineData("{'type':1,'headers':{\"Foo\": []},'target':'test',arguments:[]}", "Expected header 'Foo' to be of type String.")]

        [InlineData("{'type':1}", "Missing required property 'target'.")]
        [InlineData("{'type':1,'invocationId':42}", "Expected 'invocationId' to be of type String.")]
        [InlineData("{'type':1,'invocationId':'42'}", "Missing required property 'target'.")]
        [InlineData("{'type':1,'invocationId':'42','target':42}", "Expected 'target' to be of type String.")]
        [InlineData("{'type':1,'invocationId':'42','target':'foo'}", "Missing required property 'arguments'.")]
        [InlineData("{'type':1,'invocationId':'42','target':'foo','arguments':{}}", "Expected 'arguments' to be of type Array.")]

        [InlineData("{'type':2}", "Missing required property 'invocationId'.")]
        [InlineData("{'type':2,'invocationId':42}", "Expected 'invocationId' to be of type String.")]
        [InlineData("{'type':2,'invocationId':'42'}", "Missing required property 'item'.")]

        [InlineData("{'type':3}", "Missing required property 'invocationId'.")]
        [InlineData("{'type':3,'invocationId':42}", "Expected 'invocationId' to be of type String.")]
        [InlineData("{'type':3,'invocationId':'42','error':[]}", "Expected 'error' to be of type String.")]

        [InlineData("{'type':4}", "Missing required property 'invocationId'.")]
        [InlineData("{'type':4,'invocationId':42}", "Expected 'invocationId' to be of type String.")]
        [InlineData("{'type':4,'invocationId':'42','target':42}", "Expected 'target' to be of type String.")]
        [InlineData("{'type':4,'invocationId':'42','target':'foo'}", "Missing required property 'arguments'.")]
        [InlineData("{'type':4,'invocationId':'42','target':'foo','arguments':{}}", "Expected 'arguments' to be of type Array.")]

        [InlineData("{'type':'foo'}", "Expected 'type' to be of type Integer.")]

        [InlineData("{'type':3,'invocationId':'42','error':'foo','result':true}", "The 'error' and 'result' properties are mutually exclusive.")]
        [InlineData("{'type':3,'invocationId':'42','result':true", "Unexpected end when reading JSON.")]
        public void InvalidMessages(string input, string expectedMessage)
        {
            input = Frame(input);

            var binder = new TestBinder(Array.Empty<Type>(), typeof(object));
            var protocol = new NewtonsoftJsonHubProtocol();
            var data = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(input));
            var ex = Assert.Throws<InvalidDataException>(() => protocol.TryParseMessage(ref data, binder, out var _));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory]
        [MemberData(nameof(OutOfOrderJsonTestDataNames))]
        public void ParseOutOfOrderJson(string outOfOrderJsonTestDataName)
        {
            var testData = OutOfOrderJsonTestData[outOfOrderJsonTestDataName];

            var input = Frame(testData.Json);

            var binder = new TestBinder(testData.Message);
            var protocol = new NewtonsoftJsonHubProtocol();
            var data = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(input));
            protocol.TryParseMessage(ref data, binder, out var message);

            Assert.Equal(testData.Message, message, TestHubMessageEqualityComparer.Instance);
        }

        [Theory]
        [InlineData("{'type':1,'invocationId':'42','target':'foo','arguments':[],'extraParameter':'1'}")]
        public void ExtraItemsInMessageAreIgnored(string input)
        {
            input = Frame(input);

            var binder = new TestBinder(paramTypes: new[] { typeof(int), typeof(string) }, returnType: typeof(bool));
            var protocol = new NewtonsoftJsonHubProtocol();
            var data = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(input));
            Assert.True(protocol.TryParseMessage(ref data, binder, out var message));
            Assert.NotNull(message);
        }

        [Theory]
        [InlineData("{'type':1,'invocationId':'42','target':'foo','arguments':[]}", "Invocation provides 0 argument(s) but target expects 2.")]
        [InlineData("{'type':1,'arguments':[], 'invocationId':'42','target':'foo'}", "Invocation provides 0 argument(s) but target expects 2.")]
        [InlineData("{'type':1,'invocationId':'42','target':'foo','arguments':[ 'abc', 'xyz']}", "Error binding arguments. Make sure that the types of the provided values match the types of the hub method being invoked.")]
        [InlineData("{'type':1,'invocationId':'42','arguments':[ 'abc', 'xyz'], 'target':'foo'}", "Error binding arguments. Make sure that the types of the provided values match the types of the hub method being invoked.")]
        [InlineData("{'type':4,'invocationId':'42','target':'foo','arguments':[]}", "Invocation provides 0 argument(s) but target expects 2.")]
        [InlineData("{'type':4,'invocationId':'42','target':'foo','arguments':[ 'abc', 'xyz']}", "Error binding arguments. Make sure that the types of the provided values match the types of the hub method being invoked.")]
        [InlineData("{'type':1,'invocationId':'42','target':'foo','arguments':[1,'',{'1':1,'2':2}]}", "Invocation provides 3 argument(s) but target expects 2.")]
        [InlineData("{'type':1,'arguments':[1,'',{'1':1,'2':2}]},'invocationId':'42','target':'foo'", "Invocation provides 3 argument(s) but target expects 2.")]
        [InlineData("{'type':1,'invocationId':'42','target':'foo','arguments':[1,[]]}", "Error binding arguments. Make sure that the types of the provided values match the types of the hub method being invoked.")]
        public void ArgumentBindingErrors(string input, string expectedMessage)
        {
            input = Frame(input);

            var binder = new TestBinder(paramTypes: new[] { typeof(int), typeof(string) }, returnType: typeof(bool));
            var protocol = new NewtonsoftJsonHubProtocol();
            var data = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(input));
            protocol.TryParseMessage(ref data, binder, out var message);
            var bindingFailure = Assert.IsType<InvocationBindingFailureMessage>(message);
            Assert.Equal(expectedMessage, bindingFailure.BindingFailure.SourceException.Message);
        }

        [Theory]
        [InlineData("{'type':1,'invocationId':'42','target':'foo','arguments':['2007-03-01T13:00:00Z']}")]
        [InlineData("{'type':1,'invocationId':'42','arguments':['2007-03-01T13:00:00Z'],'target':'foo'}")]
        public void DateTimeArgumentPreservesUtcKind(string input)
        {
            var binder = new TestBinder(new[] { typeof(DateTime) });
            var protocol = new NewtonsoftJsonHubProtocol();
            var data = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(Frame(input)));
            protocol.TryParseMessage(ref data, binder, out var message);
            var invocationMessage = Assert.IsType<InvocationMessage>(message);

            Assert.Single(invocationMessage.Arguments);
            var dt = Assert.IsType<DateTime>(invocationMessage.Arguments[0]);
            Assert.Equal(DateTimeKind.Utc, dt.Kind);
        }

        [Theory]
        [InlineData("{'type':3,'invocationId':'42','target':'foo','arguments':[],'result':'2007-03-01T13:00:00Z'}")]
        [InlineData("{'type':3,'target':'foo','arguments':[],'result':'2007-03-01T13:00:00Z','invocationId':'42'}")]
        public void DateTimeReturnValuePreservesUtcKind(string input)
        {
            var binder = new TestBinder(typeof(DateTime));
            var protocol = new NewtonsoftJsonHubProtocol();
            var data = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(Frame(input)));
            protocol.TryParseMessage(ref data, binder, out var message);
            var invocationMessage = Assert.IsType<CompletionMessage>(message);

            var dt = Assert.IsType<DateTime>(invocationMessage.Result);
            Assert.Equal(DateTimeKind.Utc, dt.Kind);
        }

        [Fact]
        public void ReadToEndOfArgumentArrayOnError()
        {
            var binder = new TestBinder(new[] { typeof(string) });
            var protocol = new NewtonsoftJsonHubProtocol();
            var data = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(Frame("{'type':1,'invocationId':'42','target':'foo','arguments':[[],{'target':'foo2'}]}")));
            protocol.TryParseMessage(ref data, binder, out var message);
            var bindingFailure = Assert.IsType<InvocationBindingFailureMessage>(message);

            Assert.Equal("foo", bindingFailure.Target);
        }

        private static string Frame(string input)
        {
            var data = Encoding.UTF8.GetBytes(input);
            return Encoding.UTF8.GetString(FormatMessageToArray(data));
        }

        private static byte[] FormatMessageToArray(byte[] message)
        {
            var output = new MemoryStream();
            output.Write(message, 0, message.Length);
            output.WriteByte(TextMessageFormatter.RecordSeparator);
            return output.ToArray();
        }

        public class JsonProtocolTestData
        {
            public string Name { get; }
            public HubMessage Message { get; }
            public bool CamelCase { get; }
            public NullValueHandling NullValueHandling { get; }
            public string Json { get; }

            public JsonProtocolTestData(string name, HubMessage message, bool camelCase, NullValueHandling nullValueHandling, string json)
            {
                Name = name;
                Message = message;
                CamelCase = camelCase;
                NullValueHandling = nullValueHandling;
                Json = json;
            }

            public override string ToString() => Name;
        }
    }
}
