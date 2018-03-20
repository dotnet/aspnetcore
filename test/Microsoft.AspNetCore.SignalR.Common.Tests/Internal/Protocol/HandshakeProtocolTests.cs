// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol
{
    public class HandshakeProtocolTests
    {
        [Theory]
        [InlineData("{\"protocol\":\"dummy\"}\u001e", "dummy")]
        [InlineData("{\"protocol\":\"\"}\u001e", "")]
        [InlineData("{\"protocol\":null}\u001e", null)]
        public void ParsingHandshakeRequestMessageSuccessForValidMessages(string json, string protocol)
        {
            var message = Encoding.UTF8.GetBytes(json);

            Assert.True(HandshakeProtocol.TryParseRequestMessage(new ReadOnlySequence<byte>(message), out var deserializedMessage, out _, out _));

            Assert.Equal(protocol, deserializedMessage.Protocol);
        }

        [Theory]
        [InlineData("{\"error\":\"dummy\"}\u001e", "dummy")]
        [InlineData("{\"error\":\"\"}\u001e", "")]
        [InlineData("{\"error\":null}\u001e", null)]
        [InlineData("{}\u001e", null)]
        public void ParsingHandshakeResponseMessageSuccessForValidMessages(string json, string error)
        {
            var message = Encoding.UTF8.GetBytes(json);

            var response = HandshakeProtocol.ParseResponseMessage(message);

            Assert.Equal(error, response.Error);
        }

        [Fact]
        public void ParsingHandshakeRequestNotCompleteReturnsFalse()
        {
            var message = Encoding.UTF8.GetBytes("42");

            Assert.False(HandshakeProtocol.TryParseRequestMessage(new ReadOnlySequence<byte>(message), out _, out _, out _));
        }

        [Theory]
        [InlineData("42\u001e", "Unexpected JSON Token Type 'Integer'. Expected a JSON Object.")]
        [InlineData("\"42\"\u001e", "Unexpected JSON Token Type 'String'. Expected a JSON Object.")]
        [InlineData("null\u001e", "Unexpected JSON Token Type 'Null'. Expected a JSON Object.")]
        [InlineData("{}\u001e", "Missing required property 'protocol'.")]
        [InlineData("[]\u001e", "Unexpected JSON Token Type 'Array'. Expected a JSON Object.")]
        public void ParsingHandshakeRequestMessageThrowsForInvalidMessages(string payload, string expectedMessage)
        {
            var message = Encoding.UTF8.GetBytes(payload);

            var exception = Assert.Throws<InvalidDataException>(() =>
                Assert.True(HandshakeProtocol.TryParseRequestMessage(new ReadOnlySequence<byte>(message), out _, out _, out _)));

            Assert.Equal(expectedMessage, exception.Message);
        }

        [Theory]
        [InlineData("42", "Unexpected JSON Token Type 'Integer'. Expected a JSON Object.")]
        [InlineData("\"42\"", "Unexpected JSON Token Type 'String'. Expected a JSON Object.")]
        [InlineData("null", "Unexpected JSON Token Type 'Null'. Expected a JSON Object.")]
        [InlineData("[]", "Unexpected JSON Token Type 'Array'. Expected a JSON Object.")]
        public void ParsingHandshakeResponseMessageThrowsForInvalidMessages(string payload, string expectedMessage)
        {
            var message = Encoding.UTF8.GetBytes(payload);

            var exception = Assert.Throws<InvalidDataException>(() =>
                HandshakeProtocol.ParseResponseMessage(message));

            Assert.Equal(expectedMessage, exception.Message);
        }
    }
}
