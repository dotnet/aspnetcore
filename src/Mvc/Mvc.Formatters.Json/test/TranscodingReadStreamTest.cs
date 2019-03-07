using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters.Json
{
    public class TranscodingReadStreamTest
    {
        public static TheoryData ReadAsyncInputLatin =>
            new TheoryData<string>
            {
                "Hello world",
                new string('A', count: 4096),
                new string('A', count: 18000),
                new string('Æ', count: 2854),
               "pingüino",
            };

        public static TheoryData ReadAsyncInputUnicode =>
            new TheoryData<string>
            {
                "AbĀāĂăĄąĆŊŋŌōŎŏŐőŒœŔŕŖŗŘřŚşŠšŢţŤťŦŧŨũŪūŬŭŮůŰűŲųŴŵŶŷŸŹźŻżŽžſAbc",
               "Abcஐஒஓஔகஙசஜஞடணதநனபமயரறலளழவஷஸஹ",
               "☀☁☂☃☄★☆☇☈☉☊☋☌☍☎☏☐☑☒☓☚☛☜☝☞☟☠☡☢☣☤☥☦☧☨☩☪☫☬☭☮☯☰☱☲☳☴☵☶☷☸",
            };

        [Theory]
        [MemberData(nameof(ReadAsyncInputLatin))]
        [MemberData(nameof(ReadAsyncInputUnicode))]
        public Task ReadAsync_WorksForReadStream_WhenInputIs_Unicode(string message)
        {
            var sourceEncoding = Encoding.Unicode;
            return ReadAsyncTest(sourceEncoding, message);
        }

        [Theory]
        [MemberData(nameof(ReadAsyncInputLatin))]
        [MemberData(nameof(ReadAsyncInputUnicode))]
        public Task ReadAsync_WorksForReadStream_WhenInputIs_UTF7(string message)
        {
            var sourceEncoding = Encoding.UTF7;
            return ReadAsyncTest(sourceEncoding, message);
        }

        [Theory]
        [MemberData(nameof(ReadAsyncInputLatin))]
        public Task ReadAsync_WorksForReadStream_WhenInputIs_WesternEuropeanEncoding(string message)
        {
            // Arrange
            var sourceEncoding = Encoding.GetEncoding(28591);
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
