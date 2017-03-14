// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using Microsoft.AspNetCore.Sockets.Internal.Formatters;
using Microsoft.AspNetCore.Sockets.Tests;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Common.Tests.Internal.Formatters
{
    public class BinaryMessageParserTests
    {
        [Theory]
        [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, MessageType.Text, "")]
        [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x41, 0x42, 0x43 }, MessageType.Text, "ABC")]
        [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0B, 0x00, 0x41, 0x0A, 0x52, 0x0D, 0x43, 0x0D, 0x0A, 0x3B, 0x44, 0x45, 0x46 }, MessageType.Text, "A\nR\rC\r\n;DEF")]
        [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03 }, MessageType.Close, "")]
        [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11, 0x03, 0x43, 0x6F, 0x6E, 0x6E, 0x65, 0x63, 0x74, 0x69, 0x6F, 0x6E, 0x20, 0x43, 0x6C, 0x6F, 0x73, 0x65, 0x64 }, MessageType.Close, "Connection Closed")]
        [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02 }, MessageType.Error, "")]
        [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0C, 0x02, 0x53, 0x65, 0x72, 0x76, 0x65, 0x72, 0x20, 0x45, 0x72, 0x72, 0x6F, 0x72 }, MessageType.Error, "Server Error")]
        public void ReadTextMessage(byte[] encoded, MessageType messageType, string payload)
        {
            var parser = new MessageParser();
            var reader = new BytesReader(encoded);
            Assert.True(parser.TryParseMessage(ref reader, MessageFormat.Binary, out var message));
            Assert.Equal(reader.Index, encoded.Length);

            MessageTestUtils.AssertMessage(message, messageType, payload);
        }

        [Theory]
        [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, new byte[0])]
        [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x01, 0xAB, 0xCD, 0xEF, 0x12 }, new byte[] { 0xAB, 0xCD, 0xEF, 0x12 })]
        public void ReadBinaryMessage(byte[] encoded, byte[] payload)
        {
            var parser = new MessageParser();
            var reader = new BytesReader(encoded);
            Assert.True(parser.TryParseMessage(ref reader, MessageFormat.Binary, out var message));
            Assert.Equal(reader.Index, encoded.Length);

            MessageTestUtils.AssertMessage(message, MessageType.Binary, payload);
        }

        [Theory]
        [InlineData(0)] // No chunking
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(256)]
        public void ReadMultipleMessages(int chunkSize)
        {
            var encoded = new byte[]
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
            var parser = new MessageParser();
            var buffer = chunkSize > 0 ?
                encoded.ToChunkedReadOnlyBytes(chunkSize) :
                new ReadOnlyBytes(encoded);
            var reader = new BytesReader(buffer);

            var messages = new List<Message>();
            while (parser.TryParseMessage(ref reader, MessageFormat.Binary, out var message))
            {
                messages.Add(message);
            }

            Assert.Equal(encoded.Length, reader.Index);

            Assert.Equal(4, messages.Count);
            MessageTestUtils.AssertMessage(messages[0], MessageType.Binary, new byte[0]);
            MessageTestUtils.AssertMessage(messages[1], MessageType.Text, "Hello,\r\nWorld!");
            MessageTestUtils.AssertMessage(messages[2], MessageType.Close, "A");
            MessageTestUtils.AssertMessage(messages[3], MessageType.Error, "Server Error");
        }

        [Theory]
        [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04 }, "Unknown type value: 0x4")] // Invalid Type
        public void ReadInvalidMessages(byte[] encoded, string message)
        {
            var parser = new MessageParser();
            var reader = new BytesReader(new ReadOnlyBytes(encoded));
            var ex = Assert.Throws<FormatException>(() => parser.TryParseMessage(ref reader, MessageFormat.Binary, out _));
            Assert.Equal(message, ex.Message);
        }

        [Theory]
        [InlineData(new byte[0])] // Empty
        [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })] // Just length
        [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x09, 0x00, 0x00, 0x00 })] // Not enough data for payload
        public void ReadIncompleteMessages(byte[] encoded)
        {
            var parser = new MessageParser();
            var reader = new BytesReader(new ReadOnlyBytes(encoded));
            Assert.False(parser.TryParseMessage(ref reader, MessageFormat.Binary, out var message));
            Assert.Equal(encoded.Length, reader.Index);
        }
    }
}
