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
                Assert.True(NegotiationProtocol.TryWriteProtocolNegotiationMessage(negotiationMessage, ms));
                Assert.True(NegotiationProtocol.TryReadProtocolNegotiationMessage(ms.ToArray(), out var deserializedMessage));

                Assert.NotNull(deserializedMessage);
                Assert.Equal(negotiationMessage.Protocol, deserializedMessage.Protocol);
            }
        }

        [Theory]
        [InlineData("2:", "Unable to parse payload as a negotiation message.")]
        [InlineData("2:42;", "Unexpected JSON Token Type 'Integer'. Expected a JSON Object.")]
        [InlineData("4:\"42\";", "Unexpected JSON Token Type 'String'. Expected a JSON Object.")]
        [InlineData("4:null;", "Unexpected JSON Token Type 'Null'. Expected a JSON Object.")]
        [InlineData("2:{};", "Missing required property 'protocol'.")]
        [InlineData("2:[];", "Unexpected JSON Token Type 'Array'. Expected a JSON Object.")]
        public void ParsingNegotiationMessageThrowsForInvalidMessages(string payload, string expectedMessage)
        {
            var message = Encoding.UTF8.GetBytes(payload);

            var exception = Assert.Throws<FormatException>(() =>
                Assert.True(NegotiationProtocol.TryReadProtocolNegotiationMessage(message, out var deserializedMessage)));

            Assert.Equal(expectedMessage, exception.Message);
        }
    }
}
