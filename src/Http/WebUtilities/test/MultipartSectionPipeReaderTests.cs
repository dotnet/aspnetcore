// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.WebUtilities
{
    public class MultipartSectionPipeReaderTest
    {
        private const string Boundary = "9051914041544843365972754266";
        // Note that CRLF (\r\n) is required. You can't use multi-line C# strings here because the line breaks on Linux are just LF.
        private const string Text = "text default";
        private const string TextAndBoundary =
Text +
"\r\n--9051914041544843365972754266--\r\n";
        private const string HtmlWithNewLines = "<!DOCTYPE html>\r\n<title>Content of a.html.</title>\r\n";
        private const string HtmlWithNewLinesAndBoundary =
HtmlWithNewLines +
"\r\n--9051914041544843365972754266--\r\n";
        private const string TextWithPartialBoundaryMatch = "text default\r\n--90519140415448433-MoreData";
        private const string TextWithPartialBoundaryMatchAndBoundary =
TextWithPartialBoundaryMatch +
"\r\n--9051914041544843365972754266--\r\n";

        private static PipeReader MakeReader(string text)
        {
            return PipeReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(text)));
        }

        private static PipeReader MakeSingleByteReader(string text)
        {
            return PipeReader.Create(new SingleByteReadStream(Encoding.UTF8.GetBytes(text)));
        }

        private static string GetString(ReadOnlySequence<byte> buffer)
        {
            return Encoding.ASCII.GetString(buffer);
        }

        [Theory]
        [InlineData(TextAndBoundary, Text)]
        [InlineData(HtmlWithNewLinesAndBoundary, HtmlWithNewLines)]
        [InlineData(TextWithPartialBoundaryMatchAndBoundary, TextWithPartialBoundaryMatch)]
        public async Task MultipartSectionPipeReader_ValidBody_Success(string input, string expected)
        {
            var pipeReader = MakeReader(input);
            var sectionReader = new MultipartSectionPipeReader(pipeReader, new MultipartBoundary(Boundary));

            var result = await sectionReader.ReadAsync();
            Assert.False(result.IsCompleted);
            Assert.False(result.IsCanceled);
            Assert.False(result.Buffer.IsEmpty);

            var actual = GetString(result.Buffer);
            Assert.Equal(expected, actual);

            sectionReader.AdvanceTo(result.Buffer.End);
            result = await sectionReader.ReadAsync();
            Assert.True(result.IsCompleted);
            Assert.False(result.IsCanceled);
            Assert.True(result.Buffer.IsEmpty);
        }

        [Theory]
        [InlineData(TextAndBoundary, Text, 1)]
        [InlineData(HtmlWithNewLinesAndBoundary, HtmlWithNewLines, 4)]
        [InlineData(TextWithPartialBoundaryMatchAndBoundary, TextWithPartialBoundaryMatch, 2)]
        public async Task MultipartSectionPipeReader_SingleByteReadAdvance_Success(string input, string expected, int reads)
        {
            var pipeReader = MakeSingleByteReader(input);
            var sectionReader = new MultipartSectionPipeReader(pipeReader, new MultipartBoundary(Boundary));
            var bufferBuilder = new List<byte>();

            for (int i = 0; i < reads; i++)
            {
                var temp = await sectionReader.ReadAsync();
                Assert.False(temp.IsCompleted);
                Assert.False(temp.IsCanceled);
                Assert.False(temp.Buffer.IsEmpty);

                bufferBuilder.AddRange(temp.Buffer.ToArray());

                sectionReader.AdvanceTo(temp.Buffer.End);
            }

            var actual = GetString(new ReadOnlySequence<byte>(bufferBuilder.ToArray()));
            Assert.Equal(expected, actual);

            var result = await sectionReader.ReadAsync();
            Assert.True(result.IsCompleted);
            Assert.False(result.IsCanceled);
            Assert.True(result.Buffer.IsEmpty);
        }

        [Theory]
        [InlineData(TextAndBoundary, Text, 1)]
        [InlineData(HtmlWithNewLinesAndBoundary, HtmlWithNewLines, 52)]
        [InlineData(TextWithPartialBoundaryMatchAndBoundary, TextWithPartialBoundaryMatch, 14)]
        public async Task MultipartSectionPipeReader_SingleByteReadWithoutAdvance_Success(string input, string expected, int reads)
        {
            var pipeReader = MakeSingleByteReader(input);
            var sectionReader = new MultipartSectionPipeReader(pipeReader, new MultipartBoundary(Boundary));

            for (int i = 0; i < reads - 1; i++)
            {
                var temp = await sectionReader.ReadAsync();
                Assert.False(temp.IsCompleted);
                Assert.False(temp.IsCanceled);
                Assert.False(temp.Buffer.IsEmpty);

                sectionReader.AdvanceTo(temp.Buffer.Start, temp.Buffer.End);
            }

            var result = await sectionReader.ReadAsync();
            Assert.False(result.IsCompleted);
            Assert.False(result.IsCanceled);
            Assert.False(result.Buffer.IsEmpty);

            var actual = GetString(result.Buffer);
            Assert.Equal(expected, actual);

            sectionReader.AdvanceTo(result.Buffer.End);

            result = await sectionReader.ReadAsync();
            Assert.True(result.IsCompleted);
            Assert.False(result.IsCanceled);
            Assert.True(result.Buffer.IsEmpty);
        }

        // Returns one byte at a time to test parsing boundary logic
        private class SingleByteReadStream : Stream
        {
            public SingleByteReadStream(byte[] input)
            {
                Input = input;
            }

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => false;

            public override long Length => throw new NotImplementedException();

            public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            private byte[] Input { get; }

            private int Offset { get; set; }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (Offset >= Input.Length)
                {
                    return Task.FromResult(0);
                }

                buffer[offset] = Input[Offset];
                Offset++;
                return Task.FromResult(1);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }
        }
    }
}
