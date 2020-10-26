// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol
{
    using static HubMessageHelpers;

    public class JsonHubProtocolTests : JsonHubProtocolTestsBase
    {
        protected override IHubProtocol JsonHubProtocol => new JsonHubProtocol();

        protected override IHubProtocol GetProtocolWithOptions(bool useCamelCase, bool ignoreNullValues)
        {
            var protocolOptions = new JsonHubProtocolOptions()
            {
                PayloadSerializerOptions = new JsonSerializerOptions()
                {
                    IgnoreNullValues = ignoreNullValues,
                    PropertyNamingPolicy = useCamelCase ? JsonNamingPolicy.CamelCase : null
                }
            };

            return new JsonHubProtocol(Options.Create(protocolOptions));
        }

        [Theory]
        [InlineData("", "Error reading JSON.")]
        [InlineData("42", "Unexpected JSON Token Type 'Number'. Expected a JSON Object.")]
        [InlineData("{\"type\":\"foo\"}", "Expected 'type' to be of type Number.")]
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

            var writer = MemoryBufferWriter.Get();
            try
            {
                var protocol = GetProtocolWithOptions(testData.UseCamelCase, testData.IgnoreNullValues);
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

        [Fact(Skip = "Do we want types like Double to be cast to int automatically?")]
        public void MagicCast()
        {
            var input = Frame("{\"type\":1,\"target\":\"Method\",\"arguments\":[1.1]}");
            var expectedMessage = new InvocationMessage("Method", new object[] { 1 });

            var binder = new TestBinder(new[] { typeof(int) });
            var data = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(input));
            JsonHubProtocol.TryParseMessage(ref data, binder, out var message);

            Assert.Equal(expectedMessage, message);
        }

        [Fact]
        public void ReadCaseInsensitivePropertiesByDefault()
        {
            var input = Frame("{\"type\":2,\"invocationId\":\"123\",\"item\":{\"StrIngProp\":\"test\",\"DoublePrOp\":3.14159,\"IntProp\":43,\"DateTimeProp\":\"2019-06-03T22:00:00\",\"NuLLProp\":null,\"ByteARRProp\":\"AgQG\"}}");

            var binder = new TestBinder(null, typeof(TemporaryCustomObject));
            var data = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(input));
            JsonHubProtocol.TryParseMessage(ref data, binder, out var message);

            var streamItemMessage = Assert.IsType<StreamItemMessage>(message);
            Assert.Equal(new TemporaryCustomObject()
            {
                ByteArrProp = new byte[] { 2, 4, 6 },
                IntProp = 43,
                DoubleProp = 3.14159,
                StringProp = "test",
                DateTimeProp = DateTime.Parse("6/3/2019 10:00:00 PM")
            }, streamItemMessage.Item);
        }

        public static IDictionary<string, JsonProtocolTestData> CustomProtocolTestData => new[]
        {
            new JsonProtocolTestData("InvocationMessage_HasFloatArgument", new InvocationMessage(null, "Target", new object[] { 1, "Foo", 2.0f }), true, true, "{\"type\":1,\"target\":\"Target\",\"arguments\":[1,\"Foo\",2]}"),
            new JsonProtocolTestData("InvocationMessage_StringIsoDateArgument", new InvocationMessage("Method", new object[] { "2016-05-10T13:51:20+12:34" }), true, true, "{\"type\":1,\"target\":\"Method\",\"arguments\":[\"2016-05-10T13:51:20\\u002B12:34\"]}"),
            new JsonProtocolTestData("InvocationMessage_HasCustomArgumentWithNoCamelCase", new InvocationMessage(null, "Target", new object[] { new TemporaryCustomObject() { ByteArrProp = new byte[] { 1, 2, 3 } } }), false, true, "{\"type\":1,\"target\":\"Target\",\"arguments\":[{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"ByteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("InvocationMessage_HasCustomArgumentWithNullValueIgnore", new InvocationMessage(null, "Target", new object[] { new TemporaryCustomObject() { ByteArrProp = new byte[] { 1, 2, 3 } } }), true, true, "{\"type\":1,\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"byteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("InvocationMessage_HasCustomArgumentWithNullValueIgnoreAndNoCamelCase", new InvocationMessage(null, "Target", new object[] { new TemporaryCustomObject() { ByteArrProp = new byte[] { 1, 2, 3 } } }), false, false, "{\"type\":1,\"target\":\"Target\",\"arguments\":[{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"NullProp\":null,\"ByteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("InvocationMessage_HasCustomArgumentWithNullValueInclude", new InvocationMessage(null, "Target", new object[] { new TemporaryCustomObject() { ByteArrProp = new byte[] { 1, 2, 3 } } }), true, false, "{\"type\":1,\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("InvocationMessage_HasHeaders", AddHeaders(TestHeaders, new InvocationMessage("123", "Target", new object[] { 1, "Foo", 2.0f })), true, true, "{\"type\":1," + SerializedHeaders + ",\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[1,\"Foo\",2]}"),

            new JsonProtocolTestData("StreamItemMessage_HasCustomItemWithNoCamelCase", new StreamItemMessage("123", new TemporaryCustomObject() { ByteArrProp = new byte[] { 1, 2, 3 } }), false, true, "{\"type\":2,\"invocationId\":\"123\",\"item\":{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"ByteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("StreamItemMessage_HasCustomItemWithNullValueIgnore", new StreamItemMessage("123", new TemporaryCustomObject() { ByteArrProp = new byte[] { 1, 2, 3 } }), true, true, "{\"type\":2,\"invocationId\":\"123\",\"item\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"byteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("StreamItemMessage_HasCustomItemWithNullValueIgnoreAndNoCamelCase", new StreamItemMessage("123", new TemporaryCustomObject() { ByteArrProp = new byte[] { 1, 2, 3 } }), false, false, "{\"type\":2,\"invocationId\":\"123\",\"item\":{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"NullProp\":null,\"ByteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("StreamItemMessage_HasCustomItemWithNullValueInclude", new StreamItemMessage("123", new TemporaryCustomObject() { ByteArrProp = new byte[] { 1, 2, 3 } }), true, false, "{\"type\":2,\"invocationId\":\"123\",\"item\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("StreamItemMessage_HasFloatItem", new StreamItemMessage("123", 2.0f), true, true, "{\"type\":2,\"invocationId\":\"123\",\"item\":2}"),
            new JsonProtocolTestData("StreamItemMessage_HasHeaders", AddHeaders(TestHeaders, new StreamItemMessage("123", new TemporaryCustomObject() { ByteArrProp = new byte[] { 1, 2, 3 } })), true, false, "{\"type\":2," + SerializedHeaders + ",\"invocationId\":\"123\",\"item\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}}"),

            new JsonProtocolTestData("CompletionMessage_HasFloatResult", CompletionMessage.WithResult("123", 2.0f), true, true, "{\"type\":3,\"invocationId\":\"123\",\"result\":2}"),
            new JsonProtocolTestData("CompletionMessage_HasCustomResultWithNoCamelCase", CompletionMessage.WithResult("123", new TemporaryCustomObject() { ByteArrProp = new byte[] { 1, 2, 3 } }), false, true, "{\"type\":3,\"invocationId\":\"123\",\"result\":{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"ByteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("CompletionMessage_HasCustomResultWithNullValueIgnore", CompletionMessage.WithResult("123", new TemporaryCustomObject() { ByteArrProp = new byte[] { 1, 2, 3 } }), true, true, "{\"type\":3,\"invocationId\":\"123\",\"result\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"byteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("CompletionMessage_HasCustomResultWithNullValueIncludeAndNoCamelCase", CompletionMessage.WithResult("123", new TemporaryCustomObject() { ByteArrProp = new byte[] { 1, 2, 3 } }), false, false, "{\"type\":3,\"invocationId\":\"123\",\"result\":{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"NullProp\":null,\"ByteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("CompletionMessage_HasCustomResultWithNullValueInclude", CompletionMessage.WithResult("123", new TemporaryCustomObject() { ByteArrProp = new byte[] { 1, 2, 3 } }), true, false, "{\"type\":3,\"invocationId\":\"123\",\"result\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}}"),
            new JsonProtocolTestData("CompletionMessage_HasErrorAndCamelCase", CompletionMessage.Empty("123"), true, true, "{\"type\":3,\"invocationId\":\"123\"}"),

            new JsonProtocolTestData("StreamInvocationMessage_HasCustomArgumentWithNoCamelCase", new StreamInvocationMessage("123", "Target", new object[] { new TemporaryCustomObject() { ByteArrProp = new byte[] { 1, 2, 3 } } }), false, true, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"ByteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("StreamInvocationMessage_HasCustomArgumentWithNullValueIgnore", new StreamInvocationMessage("123", "Target", new object[] { new TemporaryCustomObject() { ByteArrProp = new byte[] { 1, 2, 3 } } }), true, true, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"byteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("StreamInvocationMessage_HasCustomArgumentWithNullValueIgnoreAndNoCamelCase", new StreamInvocationMessage("123", "Target", new object[] { new TemporaryCustomObject() { ByteArrProp = new byte[] { 1, 2, 3 } } }), false, false, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00Z\",\"NullProp\":null,\"ByteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("StreamInvocationMessage_HasCustomArgumentWithNullValueInclude", new StreamInvocationMessage("123", "Target", new object[] { new TemporaryCustomObject() { ByteArrProp = new byte[] { 1, 2, 3 } } }), true, false, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}]}"),
            new JsonProtocolTestData("StreamInvocationMessage_HasFloatArgument", new StreamInvocationMessage("123", "Target", new object[] { 1, "Foo", 2.0f }), true, true, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[1,\"Foo\",2]}"),
            new JsonProtocolTestData("StreamInvocationMessage_HasHeaders", AddHeaders(TestHeaders, new StreamInvocationMessage("123", "Target", new object[] { new TemporaryCustomObject() { ByteArrProp = new byte[] { 1, 2, 3 } } })), true, false, "{\"type\":4," + SerializedHeaders + ",\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00Z\",\"nullProp\":null,\"byteArrProp\":\"AQID\"}]}"),
        }.ToDictionary(t => t.Name);

        public static IEnumerable<object[]> CustomProtocolTestDataNames => CustomProtocolTestData.Keys.Select(name => new object[] { name });
    }

    // Revert back to CustomObject when initial values on arrays are supported
    // e.g. byte[] arr { get; set; } = byte[] { 1, 2, 3 };
    internal class TemporaryCustomObject : IEquatable<TemporaryCustomObject>
    {
        // Not intended to be a full set of things, just a smattering of sample serializations
        public string StringProp { get; set; } = "SignalR!";

        public double DoubleProp { get; set; } = 6.2831853071;

        public int IntProp { get; set; } = 42;

        public DateTime DateTimeProp { get; set; } = new DateTime(2017, 4, 11, 0, 0, 0, DateTimeKind.Utc);

        public object NullProp { get; set; } = null;

        public byte[] ByteArrProp { get; set; }

        public override bool Equals(object obj)
        {
            return obj is TemporaryCustomObject o && Equals(o);
        }

        public override int GetHashCode()
        {
            // This is never used in a hash table
            return 0;
        }

        public bool Equals(TemporaryCustomObject right)
        {
            // This allows the comparer below to properly compare the object in the test.
            return string.Equals(StringProp, right.StringProp, StringComparison.Ordinal) &&
                DoubleProp == right.DoubleProp &&
                IntProp == right.IntProp &&
                DateTime.Equals(DateTimeProp, right.DateTimeProp) &&
                NullProp == right.NullProp &&
                System.Linq.Enumerable.SequenceEqual(ByteArrProp, right.ByteArrProp);
        }
    }
}
