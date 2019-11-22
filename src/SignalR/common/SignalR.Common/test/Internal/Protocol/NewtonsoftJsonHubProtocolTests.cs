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

    public class NewtonsoftJsonHubProtocolTests : JsonHubProtocolTestsBase
    {
        protected override IHubProtocol JsonHubProtocol => new NewtonsoftJsonHubProtocol();

        protected override IHubProtocol GetProtocolWithOptions(bool useCamelCase, bool ignoreNullValues)
        {
            var protocolOptions = new NewtonsoftJsonHubProtocolOptions
            {
                PayloadSerializerSettings = new JsonSerializerSettings()
                {
                    NullValueHandling = ignoreNullValues ? NullValueHandling.Ignore : NullValueHandling.Include,
                    ContractResolver = useCamelCase ? new CamelCasePropertyNamesContractResolver() : new DefaultContractResolver()
                }
            };

            return new NewtonsoftJsonHubProtocol(Options.Create(protocolOptions));
        }

        [Theory]
        [InlineData("", "Unexpected end when reading JSON.")]
        [InlineData("42", "Unexpected JSON Token Type 'Integer'. Expected a JSON Object.")]
        [InlineData("{\"type\":\"foo\"}", "Expected 'type' to be of type Integer.")]
        public void CustomInvalidMessages(string input, string expectedMessage)
        {
            input = Frame(input);

            var binder = new TestBinder(Array.Empty<Type>(), typeof(object));
            var data = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(input));
            var ex = Assert.Throws<InvalidDataException>(() => JsonHubProtocol.TryParseMessage(ref data, binder, out var _));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory]
        [MemberData(nameof(CustomProtocolTestDataNames))]
        public void CustomWriteMessage(string protocolTestDataName)
        {
            var testData = CustomProtocolTestData[protocolTestDataName];

            var expectedOutput = Frame(testData.Json);

            var protocol = GetProtocolWithOptions(testData.UseCamelCase, testData.IgnoreNullValues);

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
        [MemberData(nameof(CustomProtocolTestDataNames))]
        public void CustomParseMessage(string protocolTestDataName)
        {
            var testData = CustomProtocolTestData[protocolTestDataName];

            var input = Frame(testData.Json);

            var binder = new TestBinder(testData.Message);
            var data = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(input));
            var protocol = GetProtocolWithOptions(testData.UseCamelCase, testData.IgnoreNullValues);
            protocol.TryParseMessage(ref data, binder, out var message);

            Assert.Equal(testData.Message, message, TestHubMessageEqualityComparer.Instance);
        }

        public static IDictionary<string, JsonProtocolTestData> CustomProtocolTestData => new[]
        {
            new JsonProtocolTestData("InvocationMessage_HasFloatArgument", new InvocationMessage(null, "Target", new object[] { 1, "Foo", 2.0f }), true, true, "{\"type\":1,\"target\":\"Target\",\"arguments\":[1,\"Foo\",2.0]}"),
            new JsonProtocolTestData("InvocationMessage_StringIsoDateArgument", new InvocationMessage("Method", new object[] { "2016-05-10T13:51:20+12:34" }), false, true, "{\"type\":1,\"target\":\"Method\",\"arguments\":[\"2016-05-10T13:51:20+12:34\"]}"),
            new JsonProtocolTestData("InvocationMessage_HasCustomArgumentWithNoCamelCase", new InvocationMessage(null, "Target", new object[] { new CustomObject() }), false, true, "{\"type\":1,\"target\":\"Target\",\"arguments\":[{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"ByteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("InvocationMessage_HasCustomArgumentWithNullValueIgnore", new InvocationMessage(null, "Target", new object[] { new CustomObject() }), true, true, "{\"type\":1,\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"byteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("InvocationMessage_HasCustomArgumentWithNullValueIgnoreAndNoCamelCase", new InvocationMessage(null, "Target", new object[] { new CustomObject() }), false, false, "{\"type\":1,\"target\":\"Target\",\"arguments\":[{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"NullProp\":null,\"ByteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("InvocationMessage_HasCustomArgumentWithNullValueInclude", new InvocationMessage(null, "Target", new object[] { new CustomObject() }), true, false, "{\"type\":1,\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("InvocationMessage_HasHeaders", AddHeaders(TestHeaders, new InvocationMessage("123", "Target", new object[] { 1, "Foo", 2.0f })), true, true, "{\"type\":1," + SerializedHeaders + ",\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[1,\"Foo\",2.0]}"),

            new JsonProtocolTestData("StreamItemMessage_HasFloatItem", new StreamItemMessage("123", 2.0f), true, true, "{\"type\":2,\"invocationId\":\"123\",\"item\":2.0}"),
            new JsonProtocolTestData("StreamItemMessage_HasCustomItemWithNoCamelCase", new StreamItemMessage("123", new CustomObject()), false, true, "{\"type\":2,\"invocationId\":\"123\",\"item\":{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"ByteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("StreamItemMessage_HasCustomItemWithNullValueIgnore", new StreamItemMessage("123", new CustomObject()), true, true, "{\"type\":2,\"invocationId\":\"123\",\"item\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"byteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("StreamItemMessage_HasCustomItemWithNullValueIgnoreAndNoCamelCase", new StreamItemMessage("123", new CustomObject()), false, false, "{\"type\":2,\"invocationId\":\"123\",\"item\":{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"NullProp\":null,\"ByteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("StreamItemMessage_HasCustomItemWithNullValueInclude", new StreamItemMessage("123", new CustomObject()), true, false, "{\"type\":2,\"invocationId\":\"123\",\"item\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("StreamItemMessage_HasHeaders", AddHeaders(TestHeaders, new StreamItemMessage("123", new CustomObject())), true, false, "{\"type\":2," + SerializedHeaders + ",\"invocationId\":\"123\",\"item\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}}"),

            new JsonProtocolTestData("CompletionMessage_HasCustomResultWithNoCamelCase", CompletionMessage.WithResult("123", new CustomObject()), false, true, "{\"type\":3,\"invocationId\":\"123\",\"result\":{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"ByteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("CompletionMessage_HasCustomResultWithNullValueIgnore", CompletionMessage.WithResult("123", new CustomObject()), true, true, "{\"type\":3,\"invocationId\":\"123\",\"result\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"byteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("CompletionMessage_HasCustomResultWithNullValueIncludeAndNoCamelCase", CompletionMessage.WithResult("123", new CustomObject()), false, false, "{\"type\":3,\"invocationId\":\"123\",\"result\":{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"NullProp\":null,\"ByteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("CompletionMessage_HasCustomResultWithNullValueInclude", CompletionMessage.WithResult("123", new CustomObject()), true, false, "{\"type\":3,\"invocationId\":\"123\",\"result\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("CompletionMessage_HasTestHeadersAndCustomItemResult", AddHeaders(TestHeaders, CompletionMessage.WithResult("123", new CustomObject())), true, false, "{\"type\":3," + SerializedHeaders + ",\"invocationId\":\"123\",\"result\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("CompletionMessage_HasErrorAndCamelCase", CompletionMessage.Empty("123"), true, true, "{\"type\":3,\"invocationId\":\"123\"}"),
            new JsonProtocolTestData("CompletionMessage_HasErrorAndHeadersAndCamelCase", AddHeaders(TestHeaders, CompletionMessage.Empty("123")), true, true, "{\"type\":3," + SerializedHeaders + ",\"invocationId\":\"123\"}"),
            new JsonProtocolTestData("CompletionMessage_HasFloatResult", CompletionMessage.WithResult("123", 2.0f), true, true, "{\"type\":3,\"invocationId\":\"123\",\"result\":2.0}"),

            new JsonProtocolTestData("StreamInvocationMessage_HasFloatArgument", new StreamInvocationMessage("123", "Target", new object[] { 1, "Foo", 2.0f }), true, true, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[1,\"Foo\",2.0]}"),
            new JsonProtocolTestData("StreamInvocationMessage_HasCustomArgumentWithNoCamelCase", new StreamInvocationMessage("123", "Target", new object[] { new CustomObject() }), false, true, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"ByteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("StreamInvocationMessage_HasCustomArgumentWithNullValueIgnore", new StreamInvocationMessage("123", "Target", new object[] { new CustomObject() }), true, true, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"byteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("StreamInvocationMessage_HasCustomArgumentWithNullValueIgnoreAndNoCamelCase", new StreamInvocationMessage("123", "Target", new object[] { new CustomObject() }), false, false, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"NullProp\":null,\"ByteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("StreamInvocationMessage_HasCustomArgumentWithNullValueInclude", new StreamInvocationMessage("123", "Target", new object[] { new CustomObject() }), true, false, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("StreamInvocationMessage_HasHeaders", AddHeaders(TestHeaders, new StreamInvocationMessage("123", "Target", new object[] { new CustomObject() })), true, false, "{\"type\":4," + SerializedHeaders + ",\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}]}"),
        }.ToDictionary(t => t.Name);

        public static IEnumerable<object[]> CustomProtocolTestDataNames => CustomProtocolTestData.Keys.Select(name => new object[] { name });
    }
}
