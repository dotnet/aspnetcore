// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters.Json
{
    public class TranscodingWriteStreamTest
    {
        public static TheoryData WriteAsyncInputLatin => 
            TranscodingReadStreamTest.GetLatinTextInput(TranscodingWriteStream.MaxCharBufferSize, TranscodingWriteStream.MaxByteBufferSize);

        public static TheoryData WriteAsyncInputUnicode => 
            TranscodingReadStreamTest.GetUnicodeText(TranscodingWriteStream.MaxCharBufferSize);

        [Theory]
        [MemberData(nameof(WriteAsyncInputLatin))]
        [MemberData(nameof(WriteAsyncInputUnicode))]
        public Task WriteAsync_Works_WhenOutputIs_UTF32(string message)
        {
            var targetEncoding = Encoding.UTF32;
            return WriteAsyncTest(targetEncoding, message);
        }

        [Theory]
        [MemberData(nameof(WriteAsyncInputLatin))]
        [MemberData(nameof(WriteAsyncInputUnicode))]
        public Task WriteAsync_Works_WhenOutputIs_Unicode(string message)
        {
            var targetEncoding = Encoding.Unicode;
            return WriteAsyncTest(targetEncoding, message);
        }

        [Theory]
        [MemberData(nameof(WriteAsyncInputLatin))]
        public Task WriteAsync_Works_WhenOutputIs_UTF7(string message)
        {
            var targetEncoding = Encoding.UTF7;
            return WriteAsyncTest(targetEncoding, message);
        }

        [Theory]
        [MemberData(nameof(WriteAsyncInputLatin))]
        public Task WriteAsync_Works_WhenOutputIs_WesternEuropeanEncoding(string message)
        {
            // Arrange
            var targetEncoding = Encoding.GetEncoding(28591);
            return WriteAsyncTest(targetEncoding, message);
        }

        [Theory]
        [MemberData(nameof(WriteAsyncInputLatin))]
        public Task WriteAsync_Works_WhenOutputIs_ASCII(string message)
        {
            // Arrange
            var targetEncoding = Encoding.ASCII;
            return WriteAsyncTest(targetEncoding, message);
        }

        private static async Task WriteAsyncTest(Encoding targetEncoding, string message)
        {
            var expected = $"{{\"Message\":\"{JavaScriptEncoder.Default.Encode(message)}\"}}";

            var model = new TestModel { Message = message };
            var stream = new MemoryStream();

            var transcodingStream = new TranscodingWriteStream(stream, targetEncoding);
            await JsonSerializer.SerializeAsync(transcodingStream, model, model.GetType());
            await transcodingStream.FinalWriteAsync(default);
            await transcodingStream.FlushAsync();

            var actual = targetEncoding.GetString(stream.ToArray());
            Assert.Equal(expected, actual, StringComparer.OrdinalIgnoreCase);
        }

        private class TestModel
        {
            public string Message { get; set; }
        }
    }
}
