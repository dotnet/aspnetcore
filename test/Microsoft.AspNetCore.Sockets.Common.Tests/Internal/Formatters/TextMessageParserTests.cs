// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Sockets.Internal.Formatters;
using Microsoft.AspNetCore.Sockets.Tests;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Common.Tests.Internal.Formatters
{
    public class TextMessageParserTests
    {
        [Theory]
        [InlineData("0:T:;", MessageType.Text, "")]
        [InlineData("3:T:ABC;", MessageType.Text, "ABC")]
        [InlineData("11:T:A\nR\rC\r\n;DEF;", MessageType.Text, "A\nR\rC\r\n;DEF")]
        [InlineData("0:C:;", MessageType.Close, "")]
        [InlineData("17:C:Connection Closed;", MessageType.Close, "Connection Closed")]
        [InlineData("0:E:;", MessageType.Error, "")]
        [InlineData("12:E:Server Error;", MessageType.Error, "Server Error")]
        public void ReadTextMessage(string encoded, MessageType messageType, string payload)
        {
            var parser = new MessageParser();
            var buffer = Encoding.UTF8.GetBytes(encoded);
            var reader = new BytesReader(buffer);

            Assert.True(parser.TryParseMessage(ref reader, MessageFormat.Text, out var message));
            Assert.Equal(reader.Index, buffer.Length);

            MessageTestUtils.AssertMessage(message, messageType, payload);
        }

        [Theory]
        [InlineData("0:B:;", new byte[0])]
        [InlineData("8:B:q83vEg==;", new byte[] { 0xAB, 0xCD, 0xEF, 0x12 })]
        [InlineData("8:B:q83vEjQ=;", new byte[] { 0xAB, 0xCD, 0xEF, 0x12, 0x34 })]
        [InlineData("8:B:q83vEjRW;", new byte[] { 0xAB, 0xCD, 0xEF, 0x12, 0x34, 0x56 })]
        public void ReadBinaryMessage(string encoded, byte[] payload)
        {
            var parser = new MessageParser();
            var buffer = Encoding.UTF8.GetBytes(encoded);
            var reader = new BytesReader(buffer);

            Assert.True(parser.TryParseMessage(ref reader, MessageFormat.Text, out var message));
            Assert.Equal(reader.Index, buffer.Length);

            MessageTestUtils.AssertMessage(message, MessageType.Binary, payload);
        }

        [Theory]
        [InlineData(0)] // Not chunked
        [InlineData(4)]
        [InlineData(8)]
        public void ReadMultipleMessages(int chunkSize)
        {
            const string encoded = "0:B:;14:T:Hello,\r\nWorld!;1:C:A;12:E:Server Error;";
            var parser = new MessageParser();
            var data = Encoding.UTF8.GetBytes(encoded);
            var buffer = chunkSize > 0 ?
                data.ToChunkedReadOnlyBytes(chunkSize) :
                new ReadOnlyBytes(data);

            var reader = new BytesReader(buffer);

            var messages = new List<Message>();
            while (parser.TryParseMessage(ref reader, MessageFormat.Text, out var message))
            {
                messages.Add(message);
            }

            Assert.Equal(reader.Index, Encoding.UTF8.GetByteCount(encoded));

            Assert.Equal(4, messages.Count);
            MessageTestUtils.AssertMessage(messages[0], MessageType.Binary, new byte[0]);
            MessageTestUtils.AssertMessage(messages[1], MessageType.Text, "Hello,\r\nWorld!");
            MessageTestUtils.AssertMessage(messages[2], MessageType.Close, "A");
            MessageTestUtils.AssertMessage(messages[3], MessageType.Error, "Server Error");
        }

        [Theory]
        [InlineData("")]
        [InlineData("ABC")]
        [InlineData("1230450945")]
        [InlineData("1:")]
        [InlineData("10")]
        [InlineData("5:T:A")]
        [InlineData("5:T:ABCDE")]
        public void ReadIncompleteMessages(string encoded)
        {
            var parser = new MessageParser();
            var buffer = Encoding.UTF8.GetBytes(encoded);
            var reader = new BytesReader(buffer);
            Assert.False(parser.TryParseMessage(ref reader, MessageFormat.Text, out _));
        }

        [Theory]
        [InlineData("X:", "Invalid length: 'X'")]
        [InlineData("5:X:ABCDEF", "Unknown message type: 'X'")]
        [InlineData("1:asdf", "Unknown message type: 'a'")]
        [InlineData("1::", "Unknown message type: ':'")]
        [InlineData("1:AB:", "Unknown message type: 'A'")]
        [InlineData("1:TA", "Missing delimiter ':' after type")]
        [InlineData("1029348109238412903849023841290834901283409128349018239048102394:X:ABCDEF", "Invalid length: '1029348109238412903849023841290834901283409128349018239048102394'")]
        [InlineData("12ab34:", "Invalid length: '12ab34'")]
        [InlineData("5:T:ABCDEF", "Missing delimiter ';' after payload")]
        public void ReadInvalidMessages(string encoded, string expectedMessage)
        {
            var parser = new MessageParser();
            var buffer = Encoding.UTF8.GetBytes(encoded);
            var reader = new BytesReader(buffer);
            var ex = Assert.Throws<FormatException>(() => parser.TryParseMessage(ref reader, MessageFormat.Text, out _));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void ReadInvalidEncodedMessage()
        {
            var parser = new MessageParser();

            // Invalid because first character is a UTF-8 "continuation" character
            // We need to include the ':' so that
            var buffer = new byte[] { 0x48, 0x65, 0x80, 0x6C, 0x6F, (byte)':' };
            var reader = new BytesReader(buffer);
            var ex = Assert.Throws<FormatException>(() => parser.TryParseMessage(ref reader, MessageFormat.Text, out _));
            Assert.Equal("Invalid length", ex.Message);
        }
    }
}
