// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
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
            var stream = new TranscodingReadStream(new MemoryStream(encoding.GetBytes(input)), encoding);
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
            var stream = new TranscodingReadStream(new MemoryStream(encoding.GetBytes(input)), encoding);
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
            var stream = new TranscodingReadStream(new MemoryStream(encoding.GetBytes(input)), encoding);
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
            var input = new string('A', 4096 + 4);
            var encoding = Encoding.Unicode;
            var stream = new TranscodingReadStream(new MemoryStream(encoding.GetBytes(input)), encoding);
            var bytes = new byte[4096];
            var expected = Encoding.UTF8.GetBytes(input.Substring(0, bytes.Length));

            // Act
            var readBytes = await stream.ReadAsync(bytes, 0, bytes.Length);

            // Assert
            Assert.Equal(bytes.Length, readBytes);
            Assert.Equal(expected, bytes);
            Assert.Equal(0, stream.ByteBufferCount);
            Assert.Equal(0, stream.CharBufferCount);
            Assert.Equal(4, stream.OverflowCount);

            readBytes = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(4, readBytes);
            Assert.Equal(0, stream.ByteBufferCount);
            Assert.Equal(0, stream.CharBufferCount);
            Assert.Equal(0, stream.OverflowCount);
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

            var model = await JsonSerializer.ReadAsync(transcodingStream, typeof(TestModel));
            var testModel = Assert.IsType<TestModel>(model);

            Assert.Equal(message, testModel.Message);
        }

        public class TestModel
        {
            public string Message { get; set; }
        }
    }
}
