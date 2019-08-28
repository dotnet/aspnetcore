// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
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
                    PropertyNamingPolicy = useCamelCase ? JsonNamingPolicy.CamelCase : null,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                }
            };

            return new JsonHubProtocol(Options.Create(protocolOptions));
        }

        [Theory]
        [InlineData("", "Error reading JSON.")]
        [InlineData("42", "Unexpected JSON Token Type 'Number'. Expected a JSON Object.")]
        [InlineData("{\"type\":\"foo\"}", "Expected 'type' to be of type Number.")]
        [InlineData("{\"type\":3,\"invocationId\":\"42\",\"result\":true", "Error reading JSON.")]
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

        public static IDictionary<string, JsonProtocolTestData> CustomProtocolTestData => new[]
        {
            new JsonProtocolTestData("InvocationMessage_HasFloatArgument", new InvocationMessage(null, "Target", new object[] { 1, "Foo", 2.0f }), true, true, "{\"type\":1,\"target\":\"Target\",\"arguments\":[1,\"Foo\",2]}"),
            new JsonProtocolTestData("InvocationMessage_HasHeaders", AddHeaders(TestHeaders, new InvocationMessage("123", "Target", new object[] { 1, "Foo", 2.0f })), true, true, "{\"type\":1," + SerializedHeaders + ",\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[1,\"Foo\",2]}"),

            new JsonProtocolTestData("StreamItemMessage_HasFloatItem", new StreamItemMessage("123", 2.0f), true, true, "{\"type\":2,\"invocationId\":\"123\",\"item\":2}"),

            new JsonProtocolTestData("CompletionMessage_HasFloatResult", CompletionMessage.WithResult("123", 2.0f), true, true, "{\"type\":3,\"invocationId\":\"123\",\"result\":2}"),

            new JsonProtocolTestData("StreamInvocationMessage_HasFloatArgument", new StreamInvocationMessage("123", "Target", new object[] { 1, "Foo", 2.0f }), true, true, "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[1,\"Foo\",2]}"),
        }.ToDictionary(t => t.Name);

        public static IEnumerable<object[]> CustomProtocolTestDataNames => CustomProtocolTestData.Keys.Select(name => new object[] { name });
    }
}
