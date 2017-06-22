// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Sockets.Internal.Formatters;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Common.Tests.Internal.Formatters
{
    public class TextMessageParserTests
    {
        [Theory]
        [InlineData(0, "0:;", "")]
        [InlineData(0, "3:ABC;", "ABC")]
        [InlineData(0, "11:A\nR\rC\r\n;DEF;", "A\nR\rC\r\n;DEF")]
        [InlineData(4, "12:Hello, World;", "Hello, World")]
        public void ReadTextMessage(int chunkSize, string encoded, string payload)
        {
            var parser = new TextMessageParser();
            var buffer = Encoding.UTF8.GetBytes(encoded);
            ReadOnlySpan<byte> span = buffer.AsSpan();

            Assert.True(parser.TryParseMessage(ref span, out var message));
            Assert.Equal(0, span.Length);
            Assert.Equal(Encoding.UTF8.GetBytes(payload), message.ToArray());
        }

        [Fact]
        public void ReadMultipleMessages()
        {
            const string encoded = "0:;14:Hello,\r\nWorld!;";
            var parser = new TextMessageParser();
            var data = Encoding.UTF8.GetBytes(encoded);
            ReadOnlySpan<byte> span = data.AsSpan();

            var messages = new List<byte[]>();
            while (parser.TryParseMessage(ref span, out var message))
            {
                messages.Add(message.ToArray());
            }

            Assert.Equal(0, span.Length);

            Assert.Equal(2, messages.Count);
            Assert.Equal(new byte[0], messages[0]);
            Assert.Equal(Encoding.UTF8.GetBytes("Hello,\r\nWorld!"), messages[1]);
        }

        [Theory]
        [InlineData("")]
        [InlineData("ABC")]
        [InlineData("1230450945")]
        [InlineData("1:")]
        [InlineData("10")]
        [InlineData("5:A")]
        [InlineData("5:ABCDE")]
        public void ReadIncompleteMessages(string encoded)
        {
            var parser = new TextMessageParser();
            var buffer = Encoding.UTF8.GetBytes(encoded);
            ReadOnlySpan<byte> span = buffer.AsSpan();
            Assert.False(parser.TryParseMessage(ref span, out _));
        }

        [Theory]
        [InlineData("X:", "Invalid length: 'X'")]
        [InlineData("1:asdf", "Missing delimiter ';' after payload")]
        [InlineData("1029348109238412903849023841290834901283409128349018239048102394:ABCDEF", "Invalid length: '1029348109238412903849023841290834901283409128349018239048102394'")]
        [InlineData("12ab34:", "Invalid length: '12ab34'")]
        [InlineData("5:ABCDEF", "Missing delimiter ';' after payload")]
        public void ReadInvalidMessages(string encoded, string expectedMessage)
        {
            var parser = new TextMessageParser();
            var buffer = Encoding.UTF8.GetBytes(encoded);
            var ex = Assert.Throws<FormatException>(() =>
            {
                ReadOnlySpan<byte> span = buffer.AsSpan();
                parser.TryParseMessage(ref span, out _);
            });
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void ReadInvalidEncodedMessage()
        {
            var parser = new TextMessageParser();

            // Invalid because first character is a UTF-8 "continuation" character
            // We need to include the ':' so that
            var buffer = new byte[] { 0x48, 0x65, 0x80, 0x6C, 0x6F, (byte)':' };
            var reader = new BytesReader(buffer);
            var ex = Assert.Throws<FormatException>(() =>
            {
                ReadOnlySpan<byte> span = buffer.AsSpan();
                parser.TryParseMessage(ref span, out _);
            });
            Assert.Equal("Invalid length: 'He�lo'", ex.Message);
        }
    }
}
