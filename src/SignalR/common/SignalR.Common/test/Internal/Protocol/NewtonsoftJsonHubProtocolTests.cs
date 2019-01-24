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
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol
{
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

            var writer = MemoryBufferWriter.Get();
            try
            {
                JsonHubProtocol.WriteMessage(testData.Message, writer);
                var json = Encoding.UTF8.GetString(writer.ToArray());

                Assert.Equal(expectedOutput, json);
            }
            finally
            {
                MemoryBufferWriter.Return(writer);
            }
        }

        public static IDictionary<string, JsonProtocolTestData> CustomProtocolTestData => new[]
        {
            new JsonProtocolTestData("InvocationMessage_HasFloatArgument", new InvocationMessage(null, "Target", new object[] { 1, "Foo", 2.0f }), "{\"type\":1,\"target\":\"Target\",\"arguments\":[1,\"Foo\",2.0]}"),
            new JsonProtocolTestData("StreamItemMessage_HasFloatItem", new StreamItemMessage("123", 2.0f), "{\"type\":2,\"invocationId\":\"123\",\"item\":2.0}"),
            new JsonProtocolTestData("CompletionMessage_HasFloatResult", CompletionMessage.WithResult("123", 2.0f), "{\"type\":3,\"invocationId\":\"123\",\"result\":2.0}"),
            new JsonProtocolTestData("StreamInvocationMessage_HasFloatArgument", new StreamInvocationMessage("123", "Target", new object[] { 1, "Foo", 2.0f }), "{\"type\":4,\"invocationId\":\"123\",\"target\":\"Target\",\"arguments\":[1,\"Foo\",2.0]}"),
            new JsonProtocolTestData("InvocationMessage_StringIsoDateArgument", new InvocationMessage("Method", new object[] { "2016-05-10T13:51:20+12:34" }), "{\"type\":1,\"target\":\"Method\",\"arguments\":[\"2016-05-10T13:51:20+12:34\"]}"),
        }.ToDictionary(t => t.Name);

        public static IEnumerable<object[]> CustomProtocolTestDataNames => CustomProtocolTestData.Keys.Select(name => new object[] { name });
    }
}
