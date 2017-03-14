// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using Microsoft.AspNetCore.Sockets.Internal.Formatters;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests.Internal.Formatters
{
    public partial class BinaryMessageFormatterTests
    {
        [Fact]
        public void WriteMultipleMessages()
        {
            var expectedEncoding = new byte[]
            {
                /* length: */ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    /* type: */ 0x01, // Binary
                    /* body: <empty> */
                /* length: */ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0E,
                    /* type: */ 0x00, // Text
                    /* body: */ 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x2C, 0x0D, 0x0A, 0x57, 0x6F, 0x72, 0x6C, 0x64, 0x21,
                /* length: */ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
                    /* type: */ 0x03, // Close
                    /* body: */ 0x41,
                /* length: */ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0C,
                    /* type: */ 0x02, // Error
                    /* body: */ 0x53, 0x65, 0x72, 0x76, 0x65, 0x72, 0x20, 0x45, 0x72, 0x72, 0x6F, 0x72
            };

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
                Assert.True(MessageFormatter.TryWriteMessage(message, output, MessageFormat.Binary));
            }

            Assert.Equal(expectedEncoding, output.ToArray());
        }

        [Theory]
        [InlineData(0, 8, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, new byte[0])]
        [InlineData(0, 8, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x01, 0xAB, 0xCD, 0xEF, 0x12 }, new byte[] { 0xAB, 0xCD, 0xEF, 0x12 })]
        [InlineData(4, 8, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, new byte[0])]
        [InlineData(4, 8, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x01, 0xAB, 0xCD, 0xEF, 0x12 }, new byte[] { 0xAB, 0xCD, 0xEF, 0x12 })]
        [InlineData(0, 256, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, new byte[0])]
        [InlineData(0, 256, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x01, 0xAB, 0xCD, 0xEF, 0x12 }, new byte[] { 0xAB, 0xCD, 0xEF, 0x12 })]
        public void WriteBinaryMessage(int offset, int chunkSize, byte[] encoded, byte[] payload)
        {
            var message = MessageTestUtils.CreateMessage(payload);
            var output = new ArrayOutput(chunkSize);

            if (offset > 0)
            {
                output.Advance(offset);
            }

            Assert.True(MessageFormatter.TryWriteMessage(message, output, MessageFormat.Binary));

            Assert.Equal(encoded, output.ToArray().Slice(offset).ToArray());
        }

        [Theory]
        [InlineData(0, 8, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, MessageType.Text, "")]
        [InlineData(0, 8, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x41, 0x42, 0x43 }, MessageType.Text, "ABC")]
        [InlineData(0, 8, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0B, 0x00, 0x41, 0x0A, 0x52, 0x0D, 0x43, 0x0D, 0x0A, 0x3B, 0x44, 0x45, 0x46 }, MessageType.Text, "A\nR\rC\r\n;DEF")]
        [InlineData(0, 8, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03 }, MessageType.Close, "")]
        [InlineData(0, 8, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11, 0x03, 0x43, 0x6F, 0x6E, 0x6E, 0x65, 0x63, 0x74, 0x69, 0x6F, 0x6E, 0x20, 0x43, 0x6C, 0x6F, 0x73, 0x65, 0x64 }, MessageType.Close, "Connection Closed")]
        [InlineData(0, 8, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02 }, MessageType.Error, "")]
        [InlineData(0, 8, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0C, 0x02, 0x53, 0x65, 0x72, 0x76, 0x65, 0x72, 0x20, 0x45, 0x72, 0x72, 0x6F, 0x72 }, MessageType.Error, "Server Error")]
        [InlineData(4, 8, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, MessageType.Text, "")]
        [InlineData(0, 256, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, MessageType.Text, "")]
        public void WriteTextMessage(int offset, int chunkSize, byte[] encoded, MessageType messageType, string payload)
        {
            var message = MessageTestUtils.CreateMessage(payload, messageType);
            var output = new ArrayOutput(chunkSize);

            if (offset > 0)
            {
                output.Advance(offset);
            }

            Assert.True(MessageFormatter.TryWriteMessage(message, output, MessageFormat.Binary));

            Assert.Equal(encoded, output.ToArray().Slice(offset).ToArray());
        }

        [Fact]
        public void WriteInvalidMessages()
        {
            var message = new Message(new byte[0], MessageType.Binary, endOfMessage: false);
            var output = new ArrayOutput(chunkSize: 8); // Use small chunks to test Advance/Enlarge and partial payload writing
            var ex = Assert.Throws<ArgumentException>(() =>
                MessageFormatter.TryWriteMessage(message, output, MessageFormat.Binary));
            Assert.Equal($"Cannot format message where endOfMessage is false using this format{Environment.NewLine}Parameter name: message", ex.Message);
            Assert.Equal("message", ex.ParamName);
        }
    }
}
