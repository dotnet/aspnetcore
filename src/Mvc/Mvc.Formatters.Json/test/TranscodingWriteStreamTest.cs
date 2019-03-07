using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters.Json
{
    public class TranscodingWriteStreamTest
    {
        public static TheoryData WriteAsyncInputLatin =>
            new TheoryData<string>
            {
                "Hello world",
                new string('A', count: 4096),
                new string('A', count: 18000),
                new string('Æ', count: 2854),
               "pingüino",
            };

        public static TheoryData WriteAsyncInputUnicode =>
            new TheoryData<string>
            {
                "AbĀāĂăĄąĆŊŋŌōŎŏŐőŒœŔŕŖŗŘřŚşŠšŢţŤťŦŧŨũŪūŬŭŮůŰűŲųŴŵŶŷŸŹźŻżŽžſAbc",
               "Abcஐஒஓஔகஙசஜஞடணதநனபமயரறலளழவஷஸஹ",
               "☀☁☂☃☄★☆☇☈☉☊☋☌☍☎☏☐☑☒☓☚☛☜☝☞☟☠☡☢☣☤☥☦☧☨☩☪☫☬☭☮☯☰☱☲☳☴☵☶☷☸",
               new string('ஐ', 3600),
            };

        [Theory]
        [MemberData(nameof(WriteAsyncInputLatin))]
        public Task WriteAsync_WorksForReadStream_WhenInputIs_Unicode(string message)
        {
            var targetEncoding = Encoding.Unicode;
            return WriteAsyncTest(targetEncoding, message);
        }

        [Theory]
        [MemberData(nameof(WriteAsyncInputLatin))]
        public Task WriteAsync_WorksForReadStream_WhenInputIs_UTF7(string message)
        {
            var targetEncoding = Encoding.UTF7;
            return WriteAsyncTest(targetEncoding, message);
        }

        [Theory]
        [MemberData(nameof(WriteAsyncInputLatin))]
        public Task WriteAsync_WorksForReadStream_WhenInputIs_WesternEuropeanEncoding(string message)
        {
            // Arrange
            var targetEncoding = Encoding.GetEncoding(28591);
            return WriteAsyncTest(targetEncoding, message);
        }

        private static async Task WriteAsyncTest(Encoding targetEncoding, string message)
        {
            var expected = $"{{\"Message\":\"{JavaScriptEncoder.Default.Encode(message)}\"}}";

            var model = new TestModel { Message = message };
            var stream = new MemoryStream();

            var transcodingStream = new TranscodingWriteStream(stream, targetEncoding);
            await JsonSerializer.WriteAsync(model, model.GetType(), transcodingStream);
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
