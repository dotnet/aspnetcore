// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
