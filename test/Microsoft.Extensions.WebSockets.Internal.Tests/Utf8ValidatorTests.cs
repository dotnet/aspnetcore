// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Text;
using Channels;
using Xunit;

namespace Microsoft.Extensions.WebSockets.Internal.Tests
{
    public class Utf8ValidatorTests
    {
        [Theory]
        [InlineData(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }, "Hello")]
        [InlineData(new byte[] { 0xC2, 0xA7, 0x31, 0x2C, 0x20, 0x39, 0x35, 0xC2, 0xA2 }, "§1, 95¢")]
        [InlineData(new byte[] { 0xE0, 0xA0, 0x80, 0xE0, 0xA4, 0x80 }, "\u0800\u0900")]
        [InlineData(new byte[] { 0xF0, 0x90, 0x80, 0x80 }, "\U00010000")]
        public void ValidSingleFramePayloads(byte[] payload, string decoded)
        {
            var validator = new Utf8Validator();
            Assert.True(validator.ValidateUtf8Frame(ReadableBuffer.Create(payload), fin: true));

            // Not really part of the test, but it ensures that the "decoded" string matches the "payload",
            // so that the "decoded" string can be used as a human-readable explanation of the string in question
            Assert.Equal(decoded, Encoding.UTF8.GetString(payload));
        }

        [Theory]
        [InlineData(new byte[] { 0x48, 0x65 }, new byte[] { 0x6C, 0x6C, 0x6F }, "Hello")]

        [InlineData(new byte[0], new byte[] { 0xC2, 0xA7 }, "§")]
        [InlineData(new byte[] { 0xC2 }, new byte[] { 0xA7 }, "§")]
        [InlineData(new byte[] { 0xC2, 0xA7 }, new byte[0], "§")]

        [InlineData(new byte[0], new byte[] { 0xC2, 0xA2 }, "¢")]
        [InlineData(new byte[] { 0xC2 }, new byte[] { 0xA2 }, "¢")]
        [InlineData(new byte[] { 0xC2, 0xA2 }, new byte[0], "¢")]

        [InlineData(new byte[0], new byte[] { 0xE0, 0xA0, 0x80 }, "\u0800")]
        [InlineData(new byte[] { 0xE0 }, new byte[] { 0xA0, 0x80 }, "\u0800")]
        [InlineData(new byte[] { 0xE0, 0xA0 }, new byte[] { 0x80 }, "\u0800")]
        [InlineData(new byte[] { 0xE0, 0xA0, 0x80 }, new byte[0], "\u0800")]

        [InlineData(new byte[0], new byte[] { 0xE0, 0xA4, 0x80 }, "\u0900")]
        [InlineData(new byte[] { 0xE0 }, new byte[] { 0xA4, 0x80 }, "\u0900")]
        [InlineData(new byte[] { 0xE0, 0xA4 }, new byte[] { 0x80 }, "\u0900")]
        [InlineData(new byte[] { 0xE0, 0xA4, 0x80 }, new byte[0], "\u0900")]

        [InlineData(new byte[0], new byte[] { 0xF0, 0x90, 0x80, 0x80 }, "\U00010000")]
        [InlineData(new byte[] { 0xF0 }, new byte[] { 0x90, 0x80, 0x80 }, "\U00010000")]
        [InlineData(new byte[] { 0xF0, 0x90 }, new byte[] { 0x80, 0x80 }, "\U00010000")]
        [InlineData(new byte[] { 0xF0, 0x90, 0x80 }, new byte[] { 0x80 }, "\U00010000")]
        [InlineData(new byte[] { 0xF0, 0x90, 0x80, 0x80 }, new byte[0], "\U00010000")]
        public void ValidMultiFramePayloads(byte[] payload1, byte[] payload2, string decoded)
        {
            var validator = new Utf8Validator();
            Assert.True(validator.ValidateUtf8Frame(ReadableBuffer.Create(payload1), fin: false));
            Assert.True(validator.ValidateUtf8Frame(ReadableBuffer.Create(payload2), fin: true));

            // Not really part of the test, but it ensures that the "decoded" string matches the "payload",
            // so that the "decoded" string can be used as a human-readable explanation of the string in question
            Assert.Equal(decoded, Encoding.UTF8.GetString(Enumerable.Concat(payload1, payload2).ToArray()));
        }

        [Theory]

        // Continuation byte as first byte of code point
        [InlineData(new byte[] { 0x48, 0x65, 0x80, 0x6C, 0x6F })]
        [InlineData(new byte[] { 0x48, 0x65, 0x99, 0x6C, 0x6F })]
        [InlineData(new byte[] { 0x48, 0x65, 0xAB, 0x6C, 0x6F })]
        [InlineData(new byte[] { 0x48, 0x65, 0xB0, 0x6C, 0x6F })]

        // Incomplete Code Point
        [InlineData(new byte[] { 0xC2 })]
        [InlineData(new byte[] { 0xE0 })]
        [InlineData(new byte[] { 0xE0, 0xA0 })]
        [InlineData(new byte[] { 0xE0, 0xA4 })]
        [InlineData(new byte[] { 0xF0, 0x90, 0x80 })]

        // Overlong Encoding

        // 'H' (1 byte char) encoded with 2, 3 and 4 bytes
        [InlineData(new byte[] { 0xC1, 0x88 })]
        [InlineData(new byte[] { 0xE0, 0x81, 0x88 })]
        [InlineData(new byte[] { 0xF0, 0x80, 0x81, 0x88 })]

        // '§' (2 byte char) encoded with 3 and 4 bytes
        [InlineData(new byte[] { 0xE0, 0x82, 0xA7 })]
        [InlineData(new byte[] { 0xF0, 0x80, 0x82, 0xA7 })]

        // '\u0800' (3 byte char) encoded with 4 bytes
        [InlineData(new byte[] { 0xF0, 0x80, 0xA0, 0x80 })]
        public void InvalidSingleFramePayloads(byte[] payload)
        {
            var validator = new Utf8Validator();
            Assert.False(validator.ValidateUtf8Frame(ReadableBuffer.Create(payload), fin: true));
        }

        [Theory]

        // Continuation byte as first byte of code point
        [InlineData(new byte[] { 0x48, 0x65 }, new byte[] { 0x80, 0x6C, 0x6F })]
        [InlineData(new byte[] { 0x48, 0x65 }, new byte[] { 0x99, 0x6C, 0x6F })]
        [InlineData(new byte[] { 0x48, 0x65 }, new byte[] { 0xAB, 0x6C, 0x6F })]
        [InlineData(new byte[] { 0x48, 0x65 }, new byte[] { 0xB0, 0x6C, 0x6F })]

        // Incomplete Code Point
        [InlineData(new byte[] { 0xC2 }, new byte[0])]
        [InlineData(new byte[] { 0xE0 }, new byte[0])]
        [InlineData(new byte[] { 0xE0, 0xA0 }, new byte[0])]
        [InlineData(new byte[] { 0xE0, 0xA4 }, new byte[0])]
        [InlineData(new byte[] { 0xF0, 0x90, 0x80 }, new byte[0])]

        // Overlong Encoding

        // 'H' (1 byte char) encoded with 2, 3 and 4 bytes
        [InlineData(new byte[] { 0xC1 }, new byte[] { 0x88 })]
        [InlineData(new byte[] { 0xE0 }, new byte[] { 0x81, 0x88 })]
        [InlineData(new byte[] { 0xF0 }, new byte[] { 0x80, 0x81, 0x88 })]

        // '§' (2 byte char) encoded with 3 and 4 bytes
        [InlineData(new byte[] { 0xE0, 0x82 }, new byte[] { 0xA7 })]
        [InlineData(new byte[] { 0xF0, 0x80 }, new byte[] { 0x82, 0xA7 })]

        // '\u0800' (3 byte char) encoded with 4 bytes
        [InlineData(new byte[] { 0xF0, 0x80 }, new byte[] { 0xA0, 0x80 })]
        public void InvalidMultiFramePayloads(byte[] payload1, byte[] payload2)
        {
            var validator = new Utf8Validator();
            Assert.True(validator.ValidateUtf8Frame(ReadableBuffer.Create(payload1), fin: false));
            Assert.False(validator.ValidateUtf8Frame(ReadableBuffer.Create(payload2), fin: true));
        }
    }
}
