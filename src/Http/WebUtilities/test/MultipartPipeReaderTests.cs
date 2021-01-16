// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.WebUtilities
{
    public class MultipartPipeReaderTests
    {
        private const string Boundary = "9051914041544843365972754266";
        // Note that CRLF (\r\n) is required. You can't use multi-line C# strings here because the line breaks on Linux are just LF.
        private const string OnePartBody =
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"text\"\r\n" +
"\r\n" +
"text default\r\n" +
"--9051914041544843365972754266--\r\n";
        private const string OnePartBodyTwoHeaders =
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"text\"\r\n" +
"Custom-header: custom-value\r\n" +
"\r\n" +
"text default\r\n" +
"--9051914041544843365972754266--\r\n";
        private const string OnePartBodyWithTrailingWhitespace =
"--9051914041544843365972754266 \t   \t   \t     \t \r\n" +
"Content-Disposition: form-data; name=\"text\"\r\n" +
"\r\n" +
"text default\r\n" +
"--9051914041544843365972754266--\r\n";
        // It's non-compliant but common to leave off the last CRLF.
        private const string OnePartBodyWithoutFinalCRLF =
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"text\"\r\n" +
"\r\n" +
"text default\r\n" +
"--9051914041544843365972754266--";
        private const string TwoPartBody =
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"text\"\r\n" +
"\r\n" +
"text default\r\n" +
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"file1\"; filename=\"a.txt\"\r\n" +
"Content-Type: text/plain\r\n" +
"\r\n" +
"Content of a.txt.\r\n" +
"\r\n" +
"--9051914041544843365972754266--\r\n";
        private const string TwoPartBodyWithUnicodeFileName =
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"text\"\r\n" +
"\r\n" +
"text default\r\n" +
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"file1\"; filename=\"a色.txt\"\r\n" +
"Content-Type: text/plain\r\n" +
"\r\n" +
"Content of a.txt.\r\n" +
"\r\n" +
"--9051914041544843365972754266--\r\n";
        private const string ThreePartBody =
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"text\"\r\n" +
"\r\n" +
"text default\r\n" +
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"file1\"; filename=\"a.txt\"\r\n" +
"Content-Type: text/plain\r\n" +
"\r\n" +
"Content of a.txt.\r\n" +
"\r\n" +
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"file2\"; filename=\"a.html\"\r\n" +
"Content-Type: text/html\r\n" +
"\r\n" +
"<!DOCTYPE html><title>Content of a.html.</title>\r\n" +
"\r\n" +
"--9051914041544843365972754266--\r\n";

        private const string TwoPartBodyIncompleteBuffer =
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"text\"\r\n" +
"\r\n" +
"text default\r\n" +
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"file1\"; filename=\"a.txt\"\r\n" +
"Content-Type: text/plain\r\n" +
"\r\n" +
"Content of a.txt.\r\n" +
"\r\n" +
"--9051914041544843365";

        private static PipeReader MakeReader(string text)
        {
            return PipeReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(text)));
        }

        private static string GetString(ReadOnlySequence<byte> buffer)
        {
            return Encoding.ASCII.GetString(buffer);
        }

        [Fact]
        public async Task MultipartPipeReader_ReadSinglePartBody_Success()
        {
            var pipeReader = MakeReader(OnePartBody);
            var reader = new MultipartPipeReader(Boundary, pipeReader);

            var section = await reader.ReadNextSectionAsync();
            Assert.NotNull(section);

            Assert.Single(section!.Headers);
            Assert.Equal("form-data; name=\"text\"", section!.Headers!["Content-Disposition"][0]);

            var result = await section!.ReadAsStringAsync();
            Assert.Equal("text default", result);

            Assert.Null(await reader.ReadNextSectionAsync());
        }

        [Fact]
        public async Task MultipartPipeReader_HeaderCountExceeded_Throws()
        {
            var pipeReader = MakeReader(OnePartBodyTwoHeaders);
            var reader = new MultipartPipeReader(Boundary, pipeReader)
            {
                HeadersCountLimit = 1,
            };

            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => reader.ReadNextSectionAsync());
            Assert.Equal("Multipart headers count limit 1 exceeded.", exception.Message);
        }

        [Fact]
        public async Task MultipartPipeReader_HeadersLengthExceeded_Throws()
        {
            var pipeReader = MakeReader(OnePartBodyTwoHeaders);
            var reader = new MultipartPipeReader(Boundary, pipeReader)
            {
                HeadersLengthLimit = 60,
            };

            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => reader.ReadNextSectionAsync());
            Assert.Equal("Line length limit 17 exceeded.", exception.Message);
        }

        [Fact]
        public async Task MultipartPipeReader_ReadSinglePartBodyWithTrailingWhitespace_Success()
        {
            var pipeReader = MakeReader(OnePartBodyWithTrailingWhitespace);
            var reader = new MultipartPipeReader(Boundary, pipeReader);

            var section = await reader.ReadNextSectionAsync();
            Assert.NotNull(section);
            Assert.Single(section!.Headers);
            Assert.Equal("form-data; name=\"text\"", section!.Headers!["Content-Disposition"][0]);

            var result = await section!.ReadAsStringAsync();
            Assert.Equal("text default", result);

            Assert.Null(await reader.ReadNextSectionAsync());
        }

        [Fact]
        public async Task MultipartPipeReader_ReadSinglePartBodyWithoutLastCRLF_Success()
        {
            var pipeReader = MakeReader(OnePartBodyWithoutFinalCRLF);
            var reader = new MultipartPipeReader(Boundary, pipeReader);

            var section = await reader.ReadNextSectionAsync();
            Assert.NotNull(section);
            Assert.Single(section!.Headers);
            Assert.Equal("form-data; name=\"text\"", section!.Headers!["Content-Disposition"][0]);

            var result = await section!.ReadAsStringAsync();
            Assert.Equal("text default", result);

            Assert.Null(await reader.ReadNextSectionAsync());
        }

        [Fact]
        public async Task MultipartPipeReader_ReadTwoPartBody_Success()
        {
            var pipeReader = MakeReader(TwoPartBody);
            var reader = new MultipartPipeReader(Boundary, pipeReader);

            var section = await reader.ReadNextSectionAsync();
            Assert.NotNull(section);
            Assert.Single(section!.Headers);
            Assert.Equal("form-data; name=\"text\"", section!.Headers!["Content-Disposition"][0]);

            var result = await section!.ReadAsStringAsync();
            Assert.Equal("text default", result);

            section = await reader.ReadNextSectionAsync();
            Assert.NotNull(section);
            Assert.Equal(2, section!.Headers?.Count);
            Assert.Equal("form-data; name=\"file1\"; filename=\"a.txt\"", section!.Headers!["Content-Disposition"][0]);
            Assert.Equal("text/plain", section!.Headers!["Content-Type"][0]);

            result = await section!.ReadAsStringAsync();
            Assert.Equal("Content of a.txt.\r\n", result);

            Assert.Null(await reader.ReadNextSectionAsync());
        }

        [Fact]
        public async Task MultipartPipeReader_ReadTwoPartBodyWithUnicodeFileName_Success()
        {
            var pipeReader = MakeReader(TwoPartBodyWithUnicodeFileName);
            var reader = new MultipartPipeReader(Boundary, pipeReader);

            var section = await reader.ReadNextSectionAsync();
            Assert.NotNull(section);
            Assert.Single(section!.Headers);
            Assert.Equal("form-data; name=\"text\"", section!.Headers!["Content-Disposition"][0]);

            var result = await section!.ReadAsStringAsync();
            Assert.Equal("text default", result);

            section = await reader.ReadNextSectionAsync();
            Assert.NotNull(section);
            Assert.Equal(2, section!.Headers?.Count);
            Assert.Equal("form-data; name=\"file1\"; filename=\"a色.txt\"", section!.Headers!["Content-Disposition"][0]);
            Assert.Equal("text/plain", section!.Headers!["Content-Type"][0]);

            result = await section!.ReadAsStringAsync();
            Assert.Equal("Content of a.txt.\r\n", result);

            Assert.Null(await reader.ReadNextSectionAsync());
        }

        [Fact]
        public async Task MultipartPipeReader_ThreePartBody_Success()
        {
            var pipeReader = MakeReader(ThreePartBody);
            var reader = new MultipartPipeReader(Boundary, pipeReader);

            var section = await reader.ReadNextSectionAsync();
            Assert.NotNull(section);
            Assert.Single(section!.Headers);
            Assert.Equal("form-data; name=\"text\"", section!.Headers!["Content-Disposition"][0]);
            var result = await section!.ReadAsStringAsync();
            Assert.Equal("text default", result);

            section = await reader.ReadNextSectionAsync();
            Assert.NotNull(section);
            Assert.Equal(2, section!.Headers?.Count);
            Assert.Equal("form-data; name=\"file1\"; filename=\"a.txt\"", section!.Headers!["Content-Disposition"][0]);
            Assert.Equal("text/plain", section!.Headers!["Content-Type"][0]);
            result = await section!.ReadAsStringAsync();
            Assert.Equal("Content of a.txt.\r\n", result) ;

            section = await reader.ReadNextSectionAsync();
            Assert.NotNull(section);
            Assert.Equal(2, section!.Headers?.Count);
            Assert.Equal("form-data; name=\"file2\"; filename=\"a.html\"", section!.Headers!["Content-Disposition"][0]);
            Assert.Equal("text/html", section!.Headers!["Content-Type"][0]);
            result = await section!.ReadAsStringAsync();
            Assert.Equal("<!DOCTYPE html><title>Content of a.html.</title>\r\n", result);

            Assert.Null(await reader.ReadNextSectionAsync());
        }

        [Fact]
        public async Task MultipartPipeReader_TwoPartBodyIncompleteBuffer_OneSectionReadsSuccessfullyThirdSectionThrows()
        {
            var pipeReader = MakeReader(TwoPartBodyIncompleteBuffer);
            var reader = new MultipartPipeReader(Boundary, pipeReader);
            var buffer = new byte[128];

            // The first section can be read successfully
            var section = await reader.ReadNextSectionAsync();
            Assert.NotNull(section);
            Assert.Single(section!.Headers);
            Assert.Equal("form-data; name=\"text\"", section!.Headers!["Content-Disposition"][0]);
            var readResult = await section.BodyReader.ReadAsync();
            Assert.Equal("text default", GetString(readResult.Buffer));
            section.BodyReader.AdvanceTo(readResult.Buffer.End);

            // The second section header can be read successfully (even though the bottom boundary is truncated)
            section = await reader.ReadNextSectionAsync();
            Assert.NotNull(section);
            Assert.Equal(2, section!.Headers?.Count);
            Assert.Equal("form-data; name=\"file1\"; filename=\"a.txt\"", section!.Headers!["Content-Disposition"][0]);
            Assert.Equal("text/plain", section!.Headers!["Content-Type"][0]);
            var read = await section!.BodyReader.ReadAsync();
            section.BodyReader.AdvanceTo(read.Buffer.End);

            await Assert.ThrowsAsync<IOException>(async () =>
            {
                // we'll be unable to ensure enough bytes are buffered to even contain a final boundary
                var section = await reader.ReadNextSectionAsync();
            });
        }

        [Fact]
        public async Task MultipartPipeReader_ReadInvalidUtf8Header_ReplacementCharacters()
        {
            var body1 =
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"text\" filename=\"a";

            var body2 =
".txt\"\r\n" +
"\r\n" +
"text default\r\n" +
"--9051914041544843365972754266--\r\n";
            var stream = new MemoryStream();
            var bytes = Encoding.UTF8.GetBytes(body1);
            stream.Write(bytes, 0, bytes.Length);

            // Write an invalid utf-8 segment in the middle
            stream.Write(new byte[] { 0xC1, 0x21 }, 0, 2);

            bytes = Encoding.UTF8.GetBytes(body2);
            stream.Write(bytes, 0, bytes.Length);
            stream.Seek(0, SeekOrigin.Begin);
            var pipeReader = PipeReader.Create(stream);
            var reader = new MultipartPipeReader(Boundary, pipeReader);

            var section = await reader.ReadNextSectionAsync();
            Assert.NotNull(section);
            Assert.Single(section?.Headers);
            Assert.Equal("form-data; name=\"text\" filename=\"a\uFFFD!.txt\"", section!.Headers!["Content-Disposition"][0]);

            var result = await section!.ReadAsStringAsync();
            Assert.Equal("text default", result);

            Assert.Null(await reader.ReadNextSectionAsync());
        }

        [Fact]
        public async Task MultipartPipeReader_ReadInvalidUtf8SurrogateHeader_ReplacementCharacters()
        {
            var body1 =
"--9051914041544843365972754266\r\n" +
"Content-Disposition: form-data; name=\"text\" filename=\"a";

            var body2 =
".txt\"\r\n" +
"\r\n" +
"text default\r\n" +
"--9051914041544843365972754266--\r\n";
            var stream = new MemoryStream();
            var bytes = Encoding.UTF8.GetBytes(body1);
            stream.Write(bytes, 0, bytes.Length);

            // Write an invalid utf-8 segment in the middle
            stream.Write(new byte[] { 0xED, 0xA0, 85 }, 0, 3);

            bytes = Encoding.UTF8.GetBytes(body2);
            stream.Write(bytes, 0, bytes.Length);
            stream.Seek(0, SeekOrigin.Begin);

            var pipeReader = PipeReader.Create(stream);
            var reader = new MultipartPipeReader(Boundary, pipeReader);

            var section = await reader.ReadNextSectionAsync();
            Assert.NotNull(section);
            Assert.Single(section!.Headers);
            Assert.Equal("form-data; name=\"text\" filename=\"a\uFFFD\uFFFDU.txt\"", section!.Headers!["Content-Disposition"][0]);

            var result = await section!.ReadAsStringAsync();
            Assert.Equal("text default", result);

            Assert.Null(await reader.ReadNextSectionAsync());
        }
    }
}
