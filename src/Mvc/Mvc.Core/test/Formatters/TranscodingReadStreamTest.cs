// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters.Json
{
    public class TranscodingReadStreamTest
    {
        [Fact]
        public async Task ReadAsync_SingleByte()
        {
            // Arrange
            var input = "Hello world";
            var encoding = Encoding.Unicode;
            using var stream = new TranscodingReadStream(new MemoryStream(encoding.GetBytes(input)), encoding);
            var bytes = new byte[4];

            // Act
            var readBytes = await stream.ReadAsync(bytes, 0, 1);

            // Assert
            Assert.Equal(1, readBytes);
            Assert.Equal((byte)'H', bytes[0]);
            Assert.Equal(0, bytes[1]);

            Assert.Equal(0, stream.ByteBufferCount);
            Assert.Equal(10, stream.CharBufferCount);
            Assert.Equal(0, stream.OverflowCount);
        }

        [Fact]
        public async Task ReadAsync_FillsBuffer()
        {
            // Arrange
            var input = "Hello world";
            var encoding = Encoding.Unicode;
            using var stream = new TranscodingReadStream(new MemoryStream(encoding.GetBytes(input)), encoding);
            var bytes = new byte[3];
            var expected = Encoding.UTF8.GetBytes(input.Substring(0, bytes.Length));

            // Act
            var readBytes = await stream.ReadAsync(bytes, 0, bytes.Length);

            // Assert
            Assert.Equal(3, readBytes);
            Assert.Equal(expected, bytes);
            Assert.Equal(0, stream.ByteBufferCount);
            Assert.Equal(8, stream.CharBufferCount);
            Assert.Equal(0, stream.OverflowCount);
        }

        [Fact]
        public async Task ReadAsync_CompletedInSecondIteration()
        {
            // Arrange
            var input = new string('A', 1024 + 10);
            var encoding = Encoding.Unicode;
            using var stream = new TranscodingReadStream(new MemoryStream(encoding.GetBytes(input)), encoding);
            var bytes = new byte[1024];
            var expected = Encoding.UTF8.GetBytes(input.Substring(0, bytes.Length));

            // Act
            var readBytes = await stream.ReadAsync(bytes, 0, bytes.Length);

            // Assert
            Assert.Equal(bytes.Length, readBytes);
            Assert.Equal(expected, bytes);
            Assert.Equal(0, stream.ByteBufferCount);
            Assert.Equal(10, stream.CharBufferCount);
            Assert.Equal(0, stream.OverflowCount);

            readBytes = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(10, readBytes);
            Assert.Equal(0, stream.ByteBufferCount);
            Assert.Equal(0, stream.CharBufferCount);
            Assert.Equal(0, stream.OverflowCount);
        }

        [Fact]
        public async Task ReadAsync_WithOverflowBuffer()
        {
            // Arrange
            // Test ensures that the overflow buffer works correctly
            var input = "☀";
            var encoding = Encoding.Unicode;
            using var stream = new TranscodingReadStream(new MemoryStream(encoding.GetBytes(input)), encoding);
            var bytes = new byte[1];
            var expected = Encoding.UTF8.GetBytes(input);

            // Act
            var readBytes = await stream.ReadAsync(bytes, 0, bytes.Length);

            // Assert
            Assert.Equal(1, readBytes);
            Assert.Equal(expected[0], bytes[0]);
            Assert.Equal(0, stream.ByteBufferCount);
            Assert.Equal(0, stream.CharBufferCount);
            Assert.Equal(2, stream.OverflowCount);

            bytes = new byte[expected.Length - 1];
            readBytes = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, readBytes);
            Assert.Equal(0, stream.ByteBufferCount);
            Assert.Equal(0, stream.CharBufferCount);
            Assert.Equal(0, stream.OverflowCount);

            readBytes = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(0, readBytes);
        }

        public static TheoryData<string> ReadAsync_WithOverflowBuffer_AtBoundariesData => new TheoryData<string>
        {
            new string('a', TranscodingReadStream.MaxCharBufferSize - 1) + "☀",
            new string('a', TranscodingReadStream.MaxCharBufferSize - 2) + "☀",
            new string('a', TranscodingReadStream.MaxCharBufferSize) + "☀",
        };

        [Theory]
        [MemberData(nameof(ReadAsync_WithOverflowBuffer_AtBoundariesData))]
        public Task ReadAsync_WithOverflowBuffer_WithBufferSize1(string input) => ReadAsync_WithOverflowBufferAtCharBufferBoundaries(input, bufferSize: 1);

        [Theory]
        [MemberData(nameof(ReadAsync_WithOverflowBuffer_AtBoundariesData))]
        public Task ReadAsync_WithOverflowBuffer_WithBufferSize2(string input) => ReadAsync_WithOverflowBufferAtCharBufferBoundaries(input, bufferSize: 1);

        private static async Task<TranscodingReadStream> ReadAsync_WithOverflowBufferAtCharBufferBoundaries(string input, int bufferSize)
        {
            // Arrange
            // Test ensures that the overflow buffer works correctly
            var encoding = Encoding.Unicode;
            var stream = new TranscodingReadStream(new MemoryStream(encoding.GetBytes(input)), encoding);
            var bytes = new byte[1];
            var expected = Encoding.UTF8.GetBytes(input);

            // Act
            int read;
            var buffer = new byte[bufferSize];
            var actual = new List<byte>();

            while ((read = await stream.ReadAsync(buffer)) != 0)
            {
                actual.AddRange(buffer);
            }

            Assert.Equal(expected, actual);
            return stream;
        }

        public static TheoryData ReadAsyncInputLatin =>
            GetLatinTextInput(TranscodingReadStream.MaxCharBufferSize, TranscodingReadStream.MaxByteBufferSize);

        public static TheoryData ReadAsyncInputUnicode =>
            GetUnicodeText(TranscodingReadStream.MaxCharBufferSize);

        internal static TheoryData GetLatinTextInput(int maxCharBufferSize, int maxByteBufferSize)
        {
            return new TheoryData<string>
            {
                "Hello world",
                string.Join(string.Empty, Enumerable.Repeat("AB", 9000)),
                new string('A', count: maxByteBufferSize),
                new string('A', count: maxCharBufferSize),
                new string('A', count: maxByteBufferSize + 1),
                new string('A', count: maxCharBufferSize + 1),
            };
        }

        internal static TheoryData GetUnicodeText(int maxCharBufferSize)
        {
            return new TheoryData<string>
            {
                new string('Æ', count: 7),
                new string('A', count: maxCharBufferSize - 1) + 'Æ',
                "AbĀāĂăĄąĆŊŋŌōŎŏŐőŒœŔŕŖŗŘřŚşŠšŢţŤťŦŧŨũŪūŬŭŮůŰűŲųŴŵŶŷŸŹźŻżŽžſAbc",
               "Abcஐஒஓஔகஙசஜஞடணதநனபமயரறலளழவஷஸஹ",
               "☀☁☂☃☄★☆☇☈☉☊☋☌☍☎☏☐☑☒☓☚☛☜☝☞☟☠☡☢☣☤☥☦☧☨☩☪☫☬☭☮☯☰☱☲☳☴☵☶☷☸",
                new string('Æ', count: 64 * 1024),
                new string('Æ', count: 64 * 1024 + 1),
               "pingüino",
                new string('ऄ', count: maxCharBufferSize + 1), // This uses 3 bytes to represent in UTF8
            };
        }

        [Theory]
        [MemberData(nameof(ReadAsyncInputLatin))]
        [MemberData(nameof(ReadAsyncInputUnicode))]
        public Task ReadAsync_Works_WhenInputIs_UTF32(string message)
        {
            var sourceEncoding = Encoding.UTF32;
            return ReadAsyncTest(sourceEncoding, message);
        }

        [Theory]
        [MemberData(nameof(ReadAsyncInputLatin))]
        [MemberData(nameof(ReadAsyncInputUnicode))]
        public Task ReadAsync_Works_WhenInputIs_Unicode(string message)
        {
            var sourceEncoding = Encoding.Unicode;
            return ReadAsyncTest(sourceEncoding, message);
        }

        [Theory]
        [MemberData(nameof(ReadAsyncInputLatin))]
        [MemberData(nameof(ReadAsyncInputUnicode))]
        public Task ReadAsync_Works_WhenInputIs_UTF7(string message)
        {
            var sourceEncoding = Encoding.UTF7;
            return ReadAsyncTest(sourceEncoding, message);
        }

        [Theory]
        [MemberData(nameof(ReadAsyncInputLatin))]
        public Task ReadAsync_Works_WhenInputIs_WesternEuropeanEncoding(string message)
        {
            // Arrange
            var sourceEncoding = Encoding.GetEncoding(28591);
            return ReadAsyncTest(sourceEncoding, message);
        }

        [Theory]
        [MemberData(nameof(ReadAsyncInputLatin))]
        public Task ReadAsync_Works_WhenInputIs_ASCII(string message)
        {
            // Arrange
            var sourceEncoding = Encoding.ASCII;
            return ReadAsyncTest(sourceEncoding, message);
        }

        private static async Task ReadAsyncTest(Encoding sourceEncoding, string message)
        {
            var input = $"{{ \"Message\": \"{message}\" }}";
            var stream = new MemoryStream(sourceEncoding.GetBytes(input));

            var transcodingStream = new TranscodingReadStream(stream, sourceEncoding);

            var model = await JsonSerializer.DeserializeAsync(transcodingStream, typeof(TestModel));
            var testModel = Assert.IsType<TestModel>(model);

            Assert.Equal(message, testModel.Message);
        }

        public class TestModel
        {
            public string Message { get; set; }
        }
    }
}
