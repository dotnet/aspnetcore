// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol;

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
                DefaultIgnoreCondition = ignoreNullValues ? System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault : System.Text.Json.Serialization.JsonIgnoreCondition.Never,
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
    [InlineData("{\"type\":8,\"sequenceId\":true}", "Expected 'sequenceId' to be of type Number.")]
    [InlineData("{\"type\":9,\"sequenceId\":\"value\"}", "Expected 'sequenceId' to be of type Number.")]
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
    public void PolymorphicWorksWithInvocation()
    {
        Person todo = new JsonPersonExtended()
        {
            Name = "Person",
            Age = 99,
        };

        var expectedJson = "{\"type\":1,\"target\":\"method\",\"arguments\":[{\"$type\":\"JsonPersonExtended\",\"age\":99,\"name\":\"Person\",\"child\":null,\"parent\":null}]}";

        var writer = MemoryBufferWriter.Get();
        try
        {
            JsonHubProtocol.WriteMessage(new InvocationMessage("method", new[] { todo }), writer);

            var json = Encoding.UTF8.GetString(writer.ToArray());
            Assert.Equal(Frame(expectedJson), json);

            var binder = new TestBinder([typeof(JsonPerson)]);
            var data = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(json));
            Assert.True(JsonHubProtocol.TryParseMessage(ref data, binder, out var message));

            var invocationMessage = Assert.IsType<InvocationMessage>(message);
            Assert.Equal(1, invocationMessage.Arguments?.Length);
            PersonEqual(todo, invocationMessage.Arguments[0]);
        }
        finally
        {
            MemoryBufferWriter.Return(writer);
        }
    }

    [Fact]
    public void PolymorphicWorksWithStreamItem()
    {
        Person todo = new JsonPersonExtended()
        {
            Name = "Person",
            Age = 99,
        };

        var expectedJson = "{\"type\":2,\"invocationId\":\"1\",\"item\":{\"$type\":\"JsonPersonExtended\",\"age\":99,\"name\":\"Person\",\"child\":null,\"parent\":null}}";

        var writer = MemoryBufferWriter.Get();
        try
        {
            JsonHubProtocol.WriteMessage(new StreamItemMessage("1", todo), writer);

            var json = Encoding.UTF8.GetString(writer.ToArray());
            Assert.Equal(Frame(expectedJson), json);

            var binder = new TestBinder(typeof(JsonPerson));
            var data = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(json));
            Assert.True(JsonHubProtocol.TryParseMessage(ref data, binder, out var message));

            var streamItemMessage = Assert.IsType<StreamItemMessage>(message);
            Assert.Equal("1", streamItemMessage.InvocationId);
            PersonEqual(todo, streamItemMessage.Item);
        }
        finally
        {
            MemoryBufferWriter.Return(writer);
        }
    }

    [Fact]
    public void PolymorphicWorksWithCompletion()
    {
        Person todo = new JsonPersonExtended2()
        {
            Name = "Person",
            Location = "Canada",
        };

        var expectedJson = "{\"type\":3,\"invocationId\":\"1\",\"result\":{\"$type\":\"JsonPersonExtended2\",\"location\":\"Canada\",\"name\":\"Person\",\"child\":null,\"parent\":null}}";

        var writer = MemoryBufferWriter.Get();
        try
        {
            JsonHubProtocol.WriteMessage(CompletionMessage.WithResult("1", todo), writer);

            var json = Encoding.UTF8.GetString(writer.ToArray());
            Assert.Equal(Frame(expectedJson), json);

            var binder = new TestBinder(typeof(JsonPerson));
            var data = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(json));
            Assert.True(JsonHubProtocol.TryParseMessage(ref data, binder, out var message));

            var completionMessage = Assert.IsType<CompletionMessage>(message);
            Assert.Equal("1", completionMessage.InvocationId);
            Assert.True(completionMessage.HasResult);
            PersonEqual(todo, completionMessage.Result);
        }
        finally
        {
            MemoryBufferWriter.Return(writer);
        }
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

    private class Person
    {
        public string Name { get; set; }
        public Person Child { get; set; }
        public Person Parent { get; set; }
    }

    [JsonPolymorphic]
    [JsonDerivedType(typeof(JsonPersonExtended), nameof(JsonPersonExtended))]
    [JsonDerivedType(typeof(JsonPersonExtended2), nameof(JsonPersonExtended2))]
    private class JsonPerson : Person
    { }

    private class JsonPersonExtended : JsonPerson
    {
        public int Age { get; set; }
    }

    private class JsonPersonExtended2 : JsonPerson
    {
        public string Location { get; set; }
    }

    private static void PersonEqual(object expected, object actual)
    {
        if (expected is null && actual is null)
        {
            return;
        }

        Assert.Equal(expected.GetType(), actual.GetType());

        if (expected is JsonPersonExtended expectedPersonExtended && actual is JsonPersonExtended actualPersonExtended)
        {
            Assert.Equal(expectedPersonExtended.Name, actualPersonExtended.Name);
            Assert.Equal(expectedPersonExtended.Age, actualPersonExtended.Age);
            PersonEqual(expectedPersonExtended.Child, actualPersonExtended.Child);
            PersonEqual(expectedPersonExtended.Parent, actualPersonExtended.Parent);
            return;
        }

        if (expected is JsonPersonExtended2 expectedPersonExtended2 && actual is JsonPersonExtended2 actualPersonExtended2)
        {
            Assert.Equal(expectedPersonExtended2.Name, actualPersonExtended2.Name);
            Assert.Equal(expectedPersonExtended2.Location, actualPersonExtended2.Location);
            PersonEqual(expectedPersonExtended2.Child, actualPersonExtended2.Child);
            PersonEqual(expectedPersonExtended2.Parent, actualPersonExtended2.Parent);
            return;
        }

        if (expected is JsonPerson expectedJsonPerson && actual is JsonPerson actualJsonPerson)
        {
            Assert.Equal(expectedJsonPerson.Name, actualJsonPerson.Name);
            PersonEqual(expectedJsonPerson.Child, actualJsonPerson.Child);
            PersonEqual(expectedJsonPerson.Parent, actualJsonPerson.Parent);
            return;
        }

        if (expected is Person expectedPerson && actual is Person actualPerson)
        {
            Assert.Equal(expectedPerson.Name, actualPerson.Name);
            PersonEqual(expectedPerson.Parent, actualPerson.Parent);
            PersonEqual(expectedPerson.Child, actualPerson.Child);
        }

        Assert.Fail("Passed in unexpected object(s)");
    }
}
