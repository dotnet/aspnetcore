// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class PipelineExtensionTests : IDisposable
    {
        // ulong.MaxValue.ToString().Length
        private const int _ulongMaxValueLength = 20;

        private readonly IPipe _pipe;
        private readonly PipeFactory _pipeFactory = new PipeFactory();

        public PipelineExtensionTests()
        {
            _pipe = _pipeFactory.Create();
        }

        public void Dispose()
        {
            _pipeFactory.Dispose();
        }

        [Theory]
        [InlineData(ulong.MinValue)]
        [InlineData(ulong.MaxValue)]
        [InlineData(4_8_15_16_23_42)]
        public async Task WritesNumericToAscii(ulong number)
        {
            var writer = _pipe.Writer.Alloc();
            writer.WriteNumeric(number);
            await writer.FlushAsync();

            var reader = await _pipe.Reader.ReadAsync();
            var numAsStr = number.ToString();
            var expected = Encoding.ASCII.GetBytes(numAsStr);
            AssertExtensions.Equal(expected, reader.Buffer.Slice(0, numAsStr.Length).ToArray());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(_ulongMaxValueLength / 2)]
        [InlineData(_ulongMaxValueLength - 1)]
        public void WritesNumericAcrossSpanBoundaries(int gapSize)
        {
            var writer = _pipe.Writer.Alloc(100);
            // almost fill up the first block
            var spacer = new Span<byte>(new byte[writer.Buffer.Length - gapSize]);
            writer.Write(spacer);

            var bufferLength = writer.Buffer.Length;
            writer.WriteNumeric(ulong.MaxValue);
            Assert.NotEqual(bufferLength, writer.Buffer.Length);

            writer.FlushAsync().GetAwaiter().GetResult();

            var reader = _pipe.Reader.ReadAsync().GetAwaiter().GetResult();
            var numAsString = ulong.MaxValue.ToString();
            var written = reader.Buffer.Slice(spacer.Length, numAsString.Length);
            Assert.False(written.IsSingleSpan, "The buffer should cross spans");
            AssertExtensions.Equal(Encoding.ASCII.GetBytes(numAsString), written.ToArray());
        }

        [Theory]
        [InlineData("\0abcxyz", new byte[] { 0, 97, 98, 99, 120, 121, 122 })]
        [InlineData("!#$%i", new byte[] { 33, 35, 36, 37, 105 })]
        [InlineData("!#$%", new byte[] { 33, 35, 36, 37 })]
        [InlineData("!#$", new byte[] { 33, 35, 36 })]
        [InlineData("!#", new byte[] { 33, 35 })]
        [InlineData("!", new byte[] { 33 })]
        // null or empty
        [InlineData("", new byte[0])]
        [InlineData(null, new byte[0])]
        public async Task EncodesAsAscii(string input, byte[] expected)
        {
            var writer = _pipe.Writer.Alloc();
            writer.WriteAsciiNoValidation(input);
            await writer.FlushAsync();
            var reader = await _pipe.Reader.ReadAsync();

            if (expected.Length > 0)
            {
                AssertExtensions.Equal(
                    expected,
                    reader.Buffer.ToArray());
            }
            else
            {
                Assert.Equal(0, reader.Buffer.Length);
            }
        }

        [Theory]
        // non-ascii characters stored in 32 bits
        [InlineData("𤭢𐐝")]
        // non-ascii characters stored in 16 bits
        [InlineData("ñ٢⛄⛵")]
        public async Task WriteAsciiNoValidationWritesOnlyOneBytePerChar(string input)
        {
            // WriteAscii doesn't validate if characters are in the ASCII range
            // but it shouldn't produce more than one byte per character
            var writer = _pipe.Writer.Alloc();
            writer.WriteAsciiNoValidation(input);
            await writer.FlushAsync();
            var reader = await _pipe.Reader.ReadAsync();

            Assert.Equal(input.Length, reader.Buffer.Length);
        }

        [Fact]
        public async Task WriteAsciiNoValidation()
        {
            const byte maxAscii = 0x7f;
            var writer = _pipe.Writer.Alloc();
            for (var i = 0; i < maxAscii; i++)
            {
                writer.WriteAsciiNoValidation(new string((char)i, 1));
            }
            await writer.FlushAsync();

            var reader = await _pipe.Reader.ReadAsync();
            var data = reader.Buffer.Slice(0, maxAscii).ToArray();
            for (var i = 0; i < maxAscii; i++)
            {
                Assert.Equal(i, data[i]);
            }
        }

        [Theory]
        [InlineData(2, 1)]
        [InlineData(3, 1)]
        [InlineData(4, 2)]
        [InlineData(5, 3)]
        [InlineData(7, 4)]
        [InlineData(8, 3)]
        [InlineData(8, 4)]
        [InlineData(8, 5)]
        [InlineData(100, 48)]
        public void WritesAsciiAcrossBlockBoundaries(int stringLength, int gapSize)
        {
            var testString = new string(' ', stringLength);
            var writer = _pipe.Writer.Alloc(100);
            // almost fill up the first block
            var spacer = new Span<byte>(new byte[writer.Buffer.Length - gapSize]);
            writer.Write(spacer);
            Assert.Equal(gapSize, writer.Buffer.Span.Length);

            var bufferLength = writer.Buffer.Length;
            writer.WriteAsciiNoValidation(testString);
            Assert.NotEqual(bufferLength, writer.Buffer.Length);

            writer.FlushAsync().GetAwaiter().GetResult();

            var reader = _pipe.Reader.ReadAsync().GetAwaiter().GetResult();
            var written = reader.Buffer.Slice(spacer.Length, stringLength);
            Assert.False(written.IsSingleSpan, "The buffer should cross spans");
            AssertExtensions.Equal(Encoding.ASCII.GetBytes(testString), written.ToArray());
        }
    }
}
