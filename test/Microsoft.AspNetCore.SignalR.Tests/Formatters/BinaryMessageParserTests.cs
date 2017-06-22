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
    public class BinaryMessageParserTests
    {
        [Theory]
        [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, "")]
        [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x41, 0x42, 0x43 }, "ABC")]
        [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0B, 0x41, 0x0A, 0x52, 0x0D, 0x43, 0x0D, 0x0A, 0x3B, 0x44, 0x45, 0x46 }, "A\nR\rC\r\n;DEF")]
        public void ReadMessage(byte[] encoded, string payload)
        {
            var parser = new BinaryMessageParser();
            ReadOnlySpan<byte> span = encoded.AsSpan();
            Assert.True(parser.TryParseMessage(ref span, out var message));
            Assert.Equal(0, span.Length);

            Assert.Equal(Encoding.UTF8.GetBytes(payload), message.ToArray());
        }

        [Theory]
        [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, new byte[0])]
        [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0xAB, 0xCD, 0xEF, 0x12 }, new byte[] { 0xAB, 0xCD, 0xEF, 0x12 })]
        public void ReadBinaryMessage(byte[] encoded, byte[] payload)
        {
            var parser = new BinaryMessageParser();
            ReadOnlySpan<byte> span = encoded.AsSpan();
            Assert.True(parser.TryParseMessage(ref span, out var message));
            Assert.Equal(0, span.Length);
            Assert.Equal(payload, message.ToArray());
        }

        [Fact]
        public void ReadMultipleMessages()
        {
            var encoded = new byte[]
            {
                /* length: */ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    /* body: <empty> */
                /* length: */ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0E,
                    /* body: */ 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x2C, 0x0D, 0x0A, 0x57, 0x6F, 0x72, 0x6C, 0x64, 0x21,
            };
            var parser = new BinaryMessageParser();
            ReadOnlySpan<byte> span = encoded.AsSpan();

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
        [InlineData(new byte[0])] // Empty
        [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x09, 0x00, 0x00 })] // Not enough data for payload
        public void ReadIncompleteMessages(byte[] encoded)
        {
            var parser = new BinaryMessageParser();
            ReadOnlySpan<byte> span = encoded.AsSpan();
            Assert.False(parser.TryParseMessage(ref span, out var message));
            Assert.Equal(0, span.Length);
        }
    }
}
