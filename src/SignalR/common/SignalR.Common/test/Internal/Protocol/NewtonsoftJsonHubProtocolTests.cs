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

        public static IDictionary<string, NewtonsoftJsonProtocolTestData> CustomProtocolTestData => new[]
        {
            new NewtonsoftJsonProtocolTestData("InvocationMessage_HasFloatArgument", new InvocationMessage(null, "Target", new object[] { 1, "Foo", 2.0f }), true, NullValueHandling.Ignore, "{\"type\":1,\"target\":\"Target\",\"arguments\":[1,\"Foo\",2.0]}"),
            new NewtonsoftJsonProtocolTestData("StreamItemMessage_HasFloatItem", new StreamItemMessage("123", 2.0f), true, NullValueHandling.Ignore, "{\"type\":2,\"invocationId\":\"123\",\"item\":2.0}"),
            new NewtonsoftJsonProtocolTestData("CompletionMessage_HasFloatResult", CompletionMessage.WithResult("123", 2.0f), true, NullValueHandling.Ignore, "{\"type\":3,\"invocationId\":\"123\",\"result\":2.0}"),
            new NewtonsoftJsonProtocolTestData("StreamInvocationMessage_HasFloatArgument", new StreamInvocationMessage("123", "Target", new object[] { 1, "Foo", 2.0f }), true, NullValueHandling.Ignore, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[1,\"Foo\",2.0]}"),
            new NewtonsoftJsonProtocolTestData("InvocationMessage_StringIsoDateArgument", new InvocationMessage("Method", new object[] { "2016-05-10T13:51:20+12:34" }), false, NullValueHandling.Ignore, "{\"type\":1,\"target\":\"Method\",\"arguments\":[\"2016-05-10T13:51:20+12:34\"]}"),
            new NewtonsoftJsonProtocolTestData("InvocationMessage_HasCustomArgumentWithNoCamelCase", new InvocationMessage(null, "Target", new object[] { new CustomObject() }), false, NullValueHandling.Ignore, "{\"type\":1,\"target\":\"Target\",\"arguments\":[{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"ByteArrProp\":\"AQID\"}]}"),
            new NewtonsoftJsonProtocolTestData("InvocationMessage_HasCustomArgumentWithNullValueIgnore", new InvocationMessage(null, "Target", new object[] { new CustomObject() }), true, NullValueHandling.Ignore, "{\"type\":1,\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"byteArrProp\":\"AQID\"}]}"),
            new NewtonsoftJsonProtocolTestData("InvocationMessage_HasCustomArgumentWithNullValueIgnoreAndNoCamelCase", new InvocationMessage(null, "Target", new object[] { new CustomObject() }), false, NullValueHandling.Include, "{\"type\":1,\"target\":\"Target\",\"arguments\":[{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"NullProp\":null,\"ByteArrProp\":\"AQID\"}]}"),
            new NewtonsoftJsonProtocolTestData("InvocationMessage_HasCustomArgumentWithNullValueInclude", new InvocationMessage(null, "Target", new object[] { new CustomObject() }), true, NullValueHandling.Include, "{\"type\":1,\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}]}"),
            new NewtonsoftJsonProtocolTestData("StreamItemMessage_HasCustomItemWithNoCamelCase", new StreamItemMessage("123", new CustomObject()), false, NullValueHandling.Ignore, "{\"type\":2,\"invocationId\":\"123\",\"item\":{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"ByteArrProp\":\"AQID\"}}"),
            new NewtonsoftJsonProtocolTestData("StreamItemMessage_HasCustomItemWithNullValueIgnore", new StreamItemMessage("123", new CustomObject()), true, NullValueHandling.Ignore, "{\"type\":2,\"invocationId\":\"123\",\"item\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"byteArrProp\":\"AQID\"}}"),
            new NewtonsoftJsonProtocolTestData("StreamItemMessage_HasCustomItemWithNullValueIgnoreAndNoCamelCase", new StreamItemMessage("123", new CustomObject()), false, NullValueHandling.Include, "{\"type\":2,\"invocationId\":\"123\",\"item\":{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"NullProp\":null,\"ByteArrProp\":\"AQID\"}}"),
            new NewtonsoftJsonProtocolTestData("StreamItemMessage_HasCustomItemWithNullValueInclude", new StreamItemMessage("123", new CustomObject()), true, NullValueHandling.Include, "{\"type\":2,\"invocationId\":\"123\",\"item\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}}"),
            new NewtonsoftJsonProtocolTestData("StreamItemMessage_HasHeaders", AddHeaders(TestHeaders, new StreamItemMessage("123", new CustomObject())), true, NullValueHandling.Include, "{\"type\":2," + SerializedHeaders + ",\"invocationId\":\"123\",\"item\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}}"),
            new NewtonsoftJsonProtocolTestData("CompletionMessage_HasCustomResultWithNoCamelCase", CompletionMessage.WithResult("123", new CustomObject()), false, NullValueHandling.Ignore, "{\"type\":3,\"invocationId\":\"123\",\"result\":{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"ByteArrProp\":\"AQID\"}}"),
            new NewtonsoftJsonProtocolTestData("CompletionMessage_HasCustomResultWithNullValueIgnore", CompletionMessage.WithResult("123", new CustomObject()), true, NullValueHandling.Ignore, "{\"type\":3,\"invocationId\":\"123\",\"result\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"byteArrProp\":\"AQID\"}}"),
            new NewtonsoftJsonProtocolTestData("CompletionMessage_HasCustomResultWithNullValueIncludeAndNoCamelCase", CompletionMessage.WithResult("123", new CustomObject()), false, NullValueHandling.Include, "{\"type\":3,\"invocationId\":\"123\",\"result\":{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"NullProp\":null,\"ByteArrProp\":\"AQID\"}}"),
            new NewtonsoftJsonProtocolTestData("CompletionMessage_HasCustomResultWithNullValueInclude", CompletionMessage.WithResult("123", new CustomObject()), true, NullValueHandling.Include, "{\"type\":3,\"invocationId\":\"123\",\"result\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}}"),
            new NewtonsoftJsonProtocolTestData("CompletionMessage_HasTestHeadersAndCustomItemResult", AddHeaders(TestHeaders, CompletionMessage.WithResult("123", new CustomObject())), true, NullValueHandling.Include, "{\"type\":3," + SerializedHeaders + ",\"invocationId\":\"123\",\"result\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}}"),
            new NewtonsoftJsonProtocolTestData("CompletionMessage_HasErrorAndCamelCase", CompletionMessage.Empty("123"), true, NullValueHandling.Ignore, "{\"type\":3,\"invocationId\":\"123\"}"),
            new NewtonsoftJsonProtocolTestData("CompletionMessage_HasErrorAndHeadersAndCamelCase", AddHeaders(TestHeaders, CompletionMessage.Empty("123")), true, NullValueHandling.Ignore, "{\"type\":3," + SerializedHeaders + ",\"invocationId\":\"123\"}"),
            new NewtonsoftJsonProtocolTestData("StreamInvocationMessage_HasCustomArgumentWithNoCamelCase", new StreamInvocationMessage("123", "Target", new object[] { new CustomObject() }), false, NullValueHandling.Ignore, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"ByteArrProp\":\"AQID\"}]}"),
            new NewtonsoftJsonProtocolTestData("StreamInvocationMessage_HasCustomArgumentWithNullValueIgnore", new StreamInvocationMessage("123", "Target", new object[] { new CustomObject() }), true, NullValueHandling.Ignore, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"byteArrProp\":\"AQID\"}]}"),
            new NewtonsoftJsonProtocolTestData("StreamInvocationMessage_HasCustomArgumentWithNullValueIgnoreAndNoCamelCase", new StreamInvocationMessage("123", "Target", new object[] { new CustomObject() }), false, NullValueHandling.Include, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"NullProp\":null,\"ByteArrProp\":\"AQID\"}]}"),
            new NewtonsoftJsonProtocolTestData("StreamInvocationMessage_HasCustomArgumentWithNullValueInclude", new StreamInvocationMessage("123", "Target", new object[] { new CustomObject() }), true, NullValueHandling.Include, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}]}"),
            new NewtonsoftJsonProtocolTestData("StreamInvocationMessage_HasHeaders", AddHeaders(TestHeaders, new StreamInvocationMessage("123", "Target", new object[] { new CustomObject() })), true, NullValueHandling.Include, "{\"type\":4," + SerializedHeaders + ",\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}]}"),
            new NewtonsoftJsonProtocolTestData("CloseMessage_HasErrorWithCamelCase", new CloseMessage("Error!"), true, NullValueHandling.Ignore, "{\"type\":7,\"error\":\"Error!\"}"),
        }.ToDictionary(t => t.Name);

        public static IEnumerable<object[]> CustomProtocolTestDataNames => CustomProtocolTestData.Keys.Select(name => new object[] { name });

        public class NewtonsoftJsonProtocolTestData : JsonProtocolTestData
        {
            public NewtonsoftJsonProtocolTestData(string name, HubMessage message, bool camelCase, NullValueHandling nullValueHandling, string json) : base(name, message, json)
            {
                CamelCase = camelCase;
                NullValueHandling = nullValueHandling;
            }

            public bool CamelCase { get; }
            public NullValueHandling NullValueHandling { get; }
        }
    }
}
