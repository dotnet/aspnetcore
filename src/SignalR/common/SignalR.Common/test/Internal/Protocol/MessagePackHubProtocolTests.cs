// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol;

using static HubMessageHelpers;

public class MessagePackHubProtocolTests : MessagePackHubProtocolTestBase
{
    protected override IHubProtocol HubProtocol => new MessagePackHubProtocol();

    [Fact]
    public void SerializerCanSerializeTypesWithNoDefaultCtor()
    {
        var result = Write(CompletionMessage.WithResult("0", new List<int> { 42 }.AsReadOnly()));
        AssertMessages(new byte[] { ArrayBytes(5), 3, 0x80, StringBytes(1), (byte)'0', 0x03, ArrayBytes(1), 42 }, result);
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void WriteAndParseDateTimeConvertsToUTC(DateTimeKind dateTimeKind)
    {
        // The messagepack Timestamp format always converts input DateTime to Utc if they are passed as "DateTimeKind.Local" :
        // https://github.com/neuecc/MessagePack-CSharp/pull/520/files#diff-ed970b3daebc708ce49f55d418075979
        var originalDateTime = new DateTime(2018, 4, 9, 0, 0, 0, dateTimeKind);
        var writer = MemoryBufferWriter.Get();

        try
        {
            HubProtocol.WriteMessage(CompletionMessage.WithResult("xyz", originalDateTime), writer);
            var bytes = new ReadOnlySequence<byte>(writer.ToArray());
            HubProtocol.TryParseMessage(ref bytes, new TestBinder(typeof(DateTime)), out var hubMessage);

            var completionMessage = Assert.IsType<CompletionMessage>(hubMessage);

            var resultDateTime = (DateTime)completionMessage.Result;
            // The messagepack Timestamp format specifies that time is stored as seconds since 1970-01-01 UTC
            // so the library has no choice but to store the time as UTC
            // https://github.com/msgpack/msgpack/blob/master/spec.md#timestamp-extension-type
            // So If the original DateTiem was a "Local" one, we create a new DateTime equivalent to the original one but converted to Utc
            var expectedUtcDateTime = (originalDateTime.Kind == DateTimeKind.Local) ? originalDateTime.ToUniversalTime() : originalDateTime;

            Assert.Equal(expectedUtcDateTime, resultDateTime);
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
            HubProtocol.WriteMessage(CompletionMessage.WithResult("xyz", dateTimeOffset), writer);
            var bytes = new ReadOnlySequence<byte>(writer.ToArray());
            HubProtocol.TryParseMessage(ref bytes, new TestBinder(typeof(DateTimeOffset)), out var hubMessage);

            var completionMessage = Assert.IsType<CompletionMessage>(hubMessage);

            var resultDateTimeOffset = (DateTimeOffset)completionMessage.Result;
            Assert.Equal(dateTimeOffset, resultDateTimeOffset);
        }
        finally
        {
            MemoryBufferWriter.Return(writer);
        }
    }

        [Fact]
    public void WriteAndSendWrittenEvent()
    {
        var result = string.Empty;
        var expectedMessage = "FinalResult";
        var options = new MessagePackHubProtocolOptions();
        options.OnMessageWritten += (message, length) =>
        {
            result = "FinalResult";
        };
        var hubProtocol = new MessagePackHubProtocol(Options.Create(options));
        var dateTimeOffset = new DateTimeOffset(new DateTime(2018, 4, 9), TimeSpan.FromHours(10));
        var writer = MemoryBufferWriter.Get();

        try
        {
            hubProtocol.WriteMessage(CompletionMessage.WithResult("xyz", dateTimeOffset), writer);
            Assert.Equal(expectedMessage, result);
        }
        finally
        {
            MemoryBufferWriter.Return(writer);
        }
    }

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

    // TestData that requires object serialization
    public static IDictionary<string, MessagePackHubProtocolTestBase.ProtocolTestData> TestData => new[]
    {
            // Completion messages
            new ProtocolTestData(
                name: "CompletionWithNoHeadersAndNullResult",
                message: CompletionMessage.WithResult("xyz", payload: null),
                binary: "lQOAo3h5egPA"),
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
            new ProtocolTestData(
                name: "CompletionWithNoHeadersAndEnumResult",
                message: CompletionMessage.WithResult("xyz", payload: TestEnum.One),
                binary: "lQOAo3h5egOjT25l"),

            // Invocation messages
            new ProtocolTestData(
                name: "InvocationWithNoHeadersNoIdAndSingleNullArg",
                message: new InvocationMessage("method", new object[] { null }),
                binary: "lgGAwKZtZXRob2SRwJA="),
            new ProtocolTestData(
                name: "InvocationWithNoHeadersNoIdIntAndEnumArgs",
                message: new InvocationMessage("method", new object[] { 42, TestEnum.One }),
                binary: "lgGAwKZtZXRob2SSKqNPbmWQ"),
            new ProtocolTestData(
                name: "InvocationWithNoHeadersNoIdAndCustomObjectArg",
                message: new InvocationMessage("method", new object[] { 42, "string", new CustomObject() }),
                binary: "lgGAwKZtZXRob2STKqZzdHJpbmeGqlN0cmluZ1Byb3CoU2lnbmFsUiGqRG91YmxlUHJvcMtAGSH7VELPEqdJbnRQcm9wKqxEYXRlVGltZVByb3DW/1jsHICoTnVsbFByb3DAq0J5dGVBcnJQcm9wxAMBAgOQ"),
            new ProtocolTestData(
                name: "InvocationWithNoHeadersNoIdAndArrayOfCustomObjectArgs",
                message: new InvocationMessage("method", new object[] { new CustomObject(), new CustomObject() }),
                binary: "lgGAwKZtZXRob2SShqpTdHJpbmdQcm9wqFNpZ25hbFIhqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqsRGF0ZVRpbWVQcm9w1v9Y7ByAqE51bGxQcm9wwKtCeXRlQXJyUHJvcMQDAQIDhqpTdHJpbmdQcm9wqFNpZ25hbFIhqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqsRGF0ZVRpbWVQcm9w1v9Y7ByAqE51bGxQcm9wwKtCeXRlQXJyUHJvcMQDAQIDkA=="),
            new ProtocolTestData(
                name: "InvocationWithHeadersNoIdAndArrayOfCustomObjectArgs",
                message: AddHeaders(TestHeaders, new InvocationMessage("method", new object[] { new CustomObject(), new CustomObject() })),
                binary: "lgGDo0Zvb6NCYXKyS2V5V2l0aApOZXcNCkxpbmVzq1N0aWxsIFdvcmtzsVZhbHVlV2l0aE5ld0xpbmVzsEFsc28KV29ya3MNCkZpbmXApm1ldGhvZJKGqlN0cmluZ1Byb3CoU2lnbmFsUiGqRG91YmxlUHJvcMtAGSH7VELPEqdJbnRQcm9wKqxEYXRlVGltZVByb3DW/1jsHICoTnVsbFByb3DAq0J5dGVBcnJQcm9wxAMBAgOGqlN0cmluZ1Byb3CoU2lnbmFsUiGqRG91YmxlUHJvcMtAGSH7VELPEqdJbnRQcm9wKqxEYXRlVGltZVByb3DW/1jsHICoTnVsbFByb3DAq0J5dGVBcnJQcm9wxAMBAgOQ"),

            // StreamItem Messages
            new ProtocolTestData(
                name: "StreamItemWithNoHeadersAndNullItem",
                message: new StreamItemMessage("xyz", item: null),
                binary: "lAKAo3h5esA="),
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

            // StreamInvocation Messages
            new ProtocolTestData(
                name: "StreamInvocationWithNoHeadersAndEnumArg",
                message: new StreamInvocationMessage("xyz", "method", new object[] { TestEnum.One }),
                binary: "lgSAo3h5eqZtZXRob2SRo09uZZA="),
            new ProtocolTestData(
                name: "StreamInvocationWithNoHeadersAndNullArg",
                message: new StreamInvocationMessage("xyz", "method", new object[] { null }),
                binary: "lgSAo3h5eqZtZXRob2SRwJA="),
            new ProtocolTestData(
                name: "StreamInvocationWithNoHeadersAndIntStringAndCustomObjectArgs",
                message: new StreamInvocationMessage("xyz", "method", new object[] { 42, "string", new CustomObject() }),
                binary: "lgSAo3h5eqZtZXRob2STKqZzdHJpbmeGqlN0cmluZ1Byb3CoU2lnbmFsUiGqRG91YmxlUHJvcMtAGSH7VELPEqdJbnRQcm9wKqxEYXRlVGltZVByb3DW/1jsHICoTnVsbFByb3DAq0J5dGVBcnJQcm9wxAMBAgOQ"),
            new ProtocolTestData(
                name: "StreamInvocationWithNoHeadersAndCustomObjectArrayArg",
                message: new StreamInvocationMessage("xyz", "method", new object[] { new CustomObject(), new CustomObject() }),
                binary: "lgSAo3h5eqZtZXRob2SShqpTdHJpbmdQcm9wqFNpZ25hbFIhqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqsRGF0ZVRpbWVQcm9w1v9Y7ByAqE51bGxQcm9wwKtCeXRlQXJyUHJvcMQDAQIDhqpTdHJpbmdQcm9wqFNpZ25hbFIhqkRvdWJsZVByb3DLQBkh+1RCzxKnSW50UHJvcCqsRGF0ZVRpbWVQcm9w1v9Y7ByAqE51bGxQcm9wwKtCeXRlQXJyUHJvcMQDAQIDkA=="),
            new ProtocolTestData(
                name: "StreamInvocationWithHeadersAndCustomObjectArrayArg",
                message: AddHeaders(TestHeaders, new StreamInvocationMessage("xyz", "method", new object[] { new CustomObject(), new CustomObject() })),
                binary: "lgSDo0Zvb6NCYXKyS2V5V2l0aApOZXcNCkxpbmVzq1N0aWxsIFdvcmtzsVZhbHVlV2l0aE5ld0xpbmVzsEFsc28KV29ya3MNCkZpbmWjeHl6pm1ldGhvZJKGqlN0cmluZ1Byb3CoU2lnbmFsUiGqRG91YmxlUHJvcMtAGSH7VELPEqdJbnRQcm9wKqxEYXRlVGltZVByb3DW/1jsHICoTnVsbFByb3DAq0J5dGVBcnJQcm9wxAMBAgOGqlN0cmluZ1Byb3CoU2lnbmFsUiGqRG91YmxlUHJvcMtAGSH7VELPEqdJbnRQcm9wKqxEYXRlVGltZVByb3DW/1jsHICoTnVsbFByb3DAq0J5dGVBcnJQcm9wxAMBAgOQ"),
        }.ToDictionary(t => t.Name);

    [Theory]
    [MemberData(nameof(TestDataNames))]
    public void ParseMessages(string testDataName)
    {
        var testData = TestData[testDataName];

        TestParseMessages(testData);
    }

    [Theory]
    [MemberData(nameof(TestDataNames))]
    public void WriteMessages(string testDataName)
    {
        var testData = TestData[testDataName];

        TestWriteMessages(testData);
    }

    public static IDictionary<string, ClientResultTestData> ClientResultData => new[]
    {
        new ClientResultTestData("SimpleResult", "lQOAo3h5egMq", typeof(int), 42),
        new ClientResultTestData("NullResult", "lQOAo3h5egPA", typeof(CustomObject), null),

        new ClientResultTestData("ComplexResult", "lQOAo3h5egOGqlN0cmluZ1Byb3CoU2lnbmFsUiGqRG91YmxlUHJvcMtAGSH7VELPEqdJbnRQcm9wKqxEYXRlVGltZVByb3DW/1jsHICoTnVsbFByb3DAq0J5dGVBcnJQcm9wxAMBAgM=", typeof(CustomObject),
            new CustomObject()),
    }.ToDictionary(t => t.Name);

    public static IEnumerable<object[]> ClientResultDataNames => ClientResultData.Keys.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(ClientResultDataNames))]
    public void RawResultRoundTripsProperly(string testDataName)
    {
        var testData = ClientResultData[testDataName];
        var bytes = Convert.FromBase64String(testData.Message);

        var binder = new TestBinder(null, typeof(RawResult));
        var input = Frame(bytes);
        var data = new ReadOnlySequence<byte>(input);
        Assert.True(HubProtocol.TryParseMessage(ref data, binder, out var message));
        var completion = Assert.IsType<CompletionMessage>(message);

        var writer = MemoryBufferWriter.Get();
        try
        {
            // WriteMessage should handle RawResult as Raw Json and write it properly
            HubProtocol.WriteMessage(completion, writer);

            // Now we check if the Raw Json was written properly and can be read using the expected type
            binder = new TestBinder(null, testData.ResultType);
            var written = writer.ToArray();
            data = new ReadOnlySequence<byte>(written);
            Assert.True(HubProtocol.TryParseMessage(ref data, binder, out message));

            completion = Assert.IsType<CompletionMessage>(message);
            Assert.Equal(testData.Result, completion.Result);
        }
        finally
        {
            MemoryBufferWriter.Return(writer);
        }
    }

    [Fact]
    public void UnexpectedClientResultGivesEmptyCompletionMessage()
    {
        var binder = new TestBinder();
        var input = Frame(Convert.FromBase64String("lQOAo3h5egPA"));
        var data = new ReadOnlySequence<byte>(input);
        Assert.True(HubProtocol.TryParseMessage(ref data, binder, out var hubMessage));

        var completion = Assert.IsType<CompletionMessage>(hubMessage);
        Assert.Null(completion.Result);
        Assert.Equal("xyz", completion.InvocationId);
    }

    [Fact]
    public void WrongTypeForClientResultGivesErrorCompletionMessage()
    {
        var binder = new TestBinder(paramTypes: null, returnType: typeof(int));
        var input = Frame(Convert.FromBase64String("lQOAo3h5egOmc3RyaW5n"));
        var data = new ReadOnlySequence<byte>(input);
        Assert.True(HubProtocol.TryParseMessage(ref data, binder, out var hubMessage));

        var completion = Assert.IsType<CompletionMessage>(hubMessage);
        Assert.Null(completion.Result);
        Assert.StartsWith("Error trying to deserialize result to Int32.", completion.Error);
        Assert.Equal("xyz", completion.InvocationId);
    }

    public class ClientResultTestData
    {
        public string Name { get; }
        public string Message { get; }
        public Type ResultType { get; }
        public object Result { get; }

        public ClientResultTestData(string name, string message, Type resultType, object result)
        {
            Name = name;
            Message = message;
            ResultType = resultType;
            Result = result;
        }

        public override string ToString() => Name;
    }
}
