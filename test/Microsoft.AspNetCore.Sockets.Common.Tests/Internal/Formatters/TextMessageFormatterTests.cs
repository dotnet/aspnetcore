// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Sockets.Internal.Formatters;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests.Internal.Formatters
{
    public class TextMessageFormatterTests
    {
        [Fact]
        public void WriteMultipleMessages()
        {
            const string expectedEncoding = "0:B:;14:T:Hello,\r\nWorld!;1:C:A;12:E:Server Error;";
            var messages = new[]
            {
                MessageTestUtils.CreateMessage(new byte[0]),
                MessageTestUtils.CreateMessage("Hello,\r\nWorld!",MessageType.Text),
                MessageTestUtils.CreateMessage("A", MessageType.Close),
                MessageTestUtils.CreateMessage("Server Error", MessageType.Error)
            };

            var output = new ArrayOutput(chunkSize: 8); // Use small chunks to test Advance/Enlarge and partial payload writing
            foreach (var message in messages)
            {
                Assert.True(MessageFormatter.TryWriteMessage(message, output, MessageFormat.Text));
            }

            Assert.Equal(expectedEncoding, Encoding.UTF8.GetString(output.ToArray()));
        }

        [Theory]
        [InlineData(8, "0:B:;", new byte[0])]
        [InlineData(8, "8:B:q83vEg==;", new byte[] { 0xAB, 0xCD, 0xEF, 0x12 })]
        [InlineData(8, "8:B:q83vEjQ=;", new byte[] { 0xAB, 0xCD, 0xEF, 0x12, 0x34 })]
        [InlineData(8, "8:B:q83vEjRW;", new byte[] { 0xAB, 0xCD, 0xEF, 0x12, 0x34, 0x56 })]
        [InlineData(256, "8:B:q83vEjRW;", new byte[] { 0xAB, 0xCD, 0xEF, 0x12, 0x34, 0x56 })]
        public void WriteBinaryMessage(int chunkSize, string encoded, byte[] payload)
        {
            var message = MessageTestUtils.CreateMessage(payload);
            var output = new ArrayOutput(chunkSize);

            Assert.True(MessageFormatter.TryWriteMessage(message, output, MessageFormat.Text));

            Assert.Equal(encoded, Encoding.UTF8.GetString(output.ToArray()));
        }

        [Theory]
        [InlineData(8, "0:T:;", MessageType.Text, "")]
        [InlineData(8, "3:T:ABC;", MessageType.Text, "ABC")]
        [InlineData(8, "11:T:A\nR\rC\r\n;DEF;", MessageType.Text, "A\nR\rC\r\n;DEF")]
        [InlineData(8, "0:C:;", MessageType.Close, "")]
        [InlineData(8, "17:C:Connection Closed;", MessageType.Close, "Connection Closed")]
        [InlineData(8, "0:E:;", MessageType.Error, "")]
        [InlineData(8, "12:E:Server Error;", MessageType.Error, "Server Error")]
        [InlineData(256, "11:T:A\nR\rC\r\n;DEF;", MessageType.Text, "A\nR\rC\r\n;DEF")]
        public void WriteTextMessage(int chunkSize, string encoded, MessageType messageType, string payload)
        {
            var message = MessageTestUtils.CreateMessage(payload, messageType);
            var output = new ArrayOutput(chunkSize); // Use small chunks to test Advance/Enlarge and partial payload writing

            Assert.True(MessageFormatter.TryWriteMessage(message, output, MessageFormat.Text));

            Assert.Equal(encoded, Encoding.UTF8.GetString(output.ToArray()));
        }

        [Fact]
        public void WriteInvalidMessages()
        {
            var message = new Message(new byte[0], MessageType.Binary, endOfMessage: false);
            var output = new ArrayOutput(chunkSize: 8); // Use small chunks to test Advance/Enlarge and partial payload writing
            var ex = Assert.Throws<ArgumentException>(() =>
                MessageFormatter.TryWriteMessage(message, output, MessageFormat.Text));
            Assert.Equal($"Cannot format message where endOfMessage is false using this format{Environment.NewLine}Parameter name: message", ex.Message);
            Assert.Equal("message", ex.ParamName);
        }
    }
}
