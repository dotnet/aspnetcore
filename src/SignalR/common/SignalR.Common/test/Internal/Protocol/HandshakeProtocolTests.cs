// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.SignalR.Protocol;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol;

public class HandshakeProtocolTests
{
    [Theory]
    [InlineData("{\"protocol\":\"dummy\",\"version\":1}\u001e", "dummy", 1)]
    [InlineData("{\"protocol\":\"\",\"version\":10}\u001e", "", 10)]
    [InlineData("{\"protocol\":\"\",\"version\":10,\"unknown\":null}\u001e", "", 10)]
    [InlineData("{\"protocol\":\"firstProtocol\",\"protocol\":\"secondProtocol\",\"version\":1}\u001e", "secondProtocol", 1)]
    [InlineData("{\"protocol\":\"firstProtocol\",\"protocol\":\"secondProtocol\",\"version\":1,\"version\":75}\u001e", "secondProtocol", 75)]
    [InlineData("{\"protocol\":\"dummy\",\"version\":1,\"ignoredField\":99}\u001e", "dummy", 1)]
    [InlineData("{\"protocol\":\"dummy\",\"version\":1}{\"protocol\":\"wrong\",\"version\":99}\u001e", "dummy", 1)]
    [InlineData("{\"protocol\":\"\\u0064ummy\",\"version\":1}\u001e", "dummy", 1)]
    [InlineData("{\"\\u0070rotoco\\u006c\":\"\\u0064ummy\",\"version\":1}\u001e", "dummy", 1)]
    public void ParsingHandshakeRequestMessageSuccessForValidMessages(string json, string protocol, int version)
    {
        var message = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(json));

        Assert.True(HandshakeProtocol.TryParseRequestMessage(ref message, out var deserializedMessage));

        Assert.Equal(protocol, deserializedMessage.Protocol);
        Assert.Equal(version, deserializedMessage.Version);
    }

    [Fact]
    public void ParsingHandshakeRequestMessageSuccessForValidMessageWithMultipleSegments()
    {
        var message = ReadOnlySequenceFactory.SegmentPerByteFactory.CreateWithContent("{\"protocol\":\"json\",\"version\":1}\u001e");

        Assert.True(HandshakeProtocol.TryParseRequestMessage(ref message, out var deserializedMessage));

        Assert.Equal("json", deserializedMessage.Protocol);
        Assert.Equal(1, deserializedMessage.Version);
    }

    [Theory]
    [InlineData("{\"error\":\"dummy\"}\u001e", "dummy")]
    [InlineData("{\"error\":\"\"}\u001e", "")]
    [InlineData("{}\u001e", null)]
    [InlineData("{\"unknown\":null}\u001e", null)]
    public void ParsingHandshakeResponseMessageSuccessForValidMessages(string json, string error)
    {
        var message = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(json));

        Assert.True(HandshakeProtocol.TryParseResponseMessage(ref message, out var response));
        Assert.Equal(error, response.Error);
    }

    [Fact]
    public void ParsingHandshakeResponseMessageSuccessForValidMessageWithMultipleSegments()
    {
        var message = ReadOnlySequenceFactory.SegmentPerByteFactory.CreateWithContent("{\"error\":\"dummy\"}\u001e");

        Assert.True(HandshakeProtocol.TryParseResponseMessage(ref message, out var response));

        Assert.Equal("dummy", response.Error);
    }

    [Fact]
    public void ParsingHandshakeRequestNotCompleteReturnsFalse()
    {
        var message = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("42"));

        Assert.False(HandshakeProtocol.TryParseRequestMessage(ref message, out _));
    }

    [Theory]
    [InlineData("42\u001e", "Unexpected JSON Token Type 'Number'. Expected a JSON Object.")]
    [InlineData("\"42\"\u001e", "Unexpected JSON Token Type 'String'. Expected a JSON Object.")]
    [InlineData("null\u001e", "Unexpected JSON Token Type 'Null'. Expected a JSON Object.")]
    [InlineData("{}\u001e", "Missing required property 'protocol'. Message content: {}")]
    [InlineData("[]\u001e", "Unexpected JSON Token Type 'Array'. Expected a JSON Object.")]
    [InlineData("{\"protocol\":\"json\"}\u001e", "Missing required property 'version'. Message content: {\"protocol\":\"json\"}")]
    [InlineData("{\"version\":1}\u001e", "Missing required property 'protocol'. Message content: {\"version\":1}")]
    [InlineData("{\"type\":4,\"invocationId\":\"42\",\"target\":\"foo\",\"arguments\":{}}\u001e", "Missing required property 'protocol'. Message content: {\"type\":4,\"invocationId\":\"42\",\"target\":\"foo\",\"arguments\":{}}")]
    [InlineData("{\"version\":\"123\"}\u001e", "Expected 'version' to be of type Number.")]
    [InlineData("{\"protocol\":null,\"version\":123}\u001e", "Expected 'protocol' to be of type String.")]
    public void ParsingHandshakeRequestMessageThrowsForInvalidMessages(string payload, string expectedMessage)
    {
        var message = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(payload));

        var exception = Assert.Throws<InvalidDataException>(() =>
            Assert.True(HandshakeProtocol.TryParseRequestMessage(ref message, out _)));

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData("42\u001e", "Unexpected JSON Token Type 'Number'. Expected a JSON Object.")]
    [InlineData("\"42\"\u001e", "Unexpected JSON Token Type 'String'. Expected a JSON Object.")]
    [InlineData("null\u001e", "Unexpected JSON Token Type 'Null'. Expected a JSON Object.")]
    [InlineData("[]\u001e", "Unexpected JSON Token Type 'Array'. Expected a JSON Object.")]
    [InlineData("{\"error\":null}\u001e", "Expected 'error' to be of type String.")]
    public void ParsingHandshakeResponseMessageThrowsForInvalidMessages(string payload, string expectedMessage)
    {
        var message = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(payload));

        var exception = Assert.Throws<InvalidDataException>(() =>
            HandshakeProtocol.TryParseResponseMessage(ref message, out _));

        Assert.Equal(expectedMessage, exception.Message);
    }
}
