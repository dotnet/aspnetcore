// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol
{
    public class NegotiationProtocolTests
    {
        [Fact]
        public void CanRoundtripNegotiation()
        {
            var negotiationMessage = new NegotiationMessage(protocol: "dummy");
            using (var ms = new MemoryStream())
            {
                NegotiationProtocol.WriteMessage(negotiationMessage, ms);
                Assert.True(NegotiationProtocol.TryParseMessage(ms.ToArray(), out var deserializedMessage));

                Assert.NotNull(deserializedMessage);
                Assert.Equal(negotiationMessage.Protocol, deserializedMessage.Protocol);
            }
        }

        [Theory]
        [InlineData("", "Unable to parse payload as a negotiation message.")]
        [InlineData("42\u001e", "Unexpected JSON Token Type 'Integer'. Expected a JSON Object.")]
        [InlineData("\"42\"\u001e", "Unexpected JSON Token Type 'String'. Expected a JSON Object.")]
        [InlineData("null\u001e", "Unexpected JSON Token Type 'Null'. Expected a JSON Object.")]
        [InlineData("{}\u001e", "Missing required property 'protocol'.")]
        [InlineData("[]\u001e", "Unexpected JSON Token Type 'Array'. Expected a JSON Object.")]
        public void ParsingNegotiationMessageThrowsForInvalidMessages(string payload, string expectedMessage)
        {
            var message = Encoding.UTF8.GetBytes(payload);

            var exception = Assert.Throws<FormatException>(() =>
                Assert.True(NegotiationProtocol.TryParseMessage(message, out var deserializedMessage)));

            Assert.Equal(expectedMessage, exception.Message);
        }
    }
}
