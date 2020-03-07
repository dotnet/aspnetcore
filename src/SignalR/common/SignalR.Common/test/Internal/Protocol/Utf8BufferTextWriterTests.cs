// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Internal;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol
{
    public class Utf8BufferTextWriterTests
    {
        [Fact]
        public void WriteChar_Unicode()
        {
            var bufferWriter = new TestMemoryBufferWriter(4096);
            var textWriter = new Utf8BufferTextWriter();
            textWriter.SetWriter(bufferWriter);

            textWriter.Write('[');
            textWriter.Flush();
            Assert.Equal(1, bufferWriter.Position);
            Assert.Equal((byte)'[', bufferWriter.CurrentSegment.Span[0]);

            textWriter.Write('"');
            textWriter.Flush();
            Assert.Equal(2, bufferWriter.Position);
            Assert.Equal((byte)'"', bufferWriter.CurrentSegment.Span[1]);

            textWriter.Write('\u00A3');
            textWriter.Flush();
            Assert.Equal(4, bufferWriter.Position);

            textWriter.Write('\u00A3');
            textWriter.Flush();
            Assert.Equal(6, bufferWriter.Position);

            textWriter.Write('"');
            textWriter.Flush();
            Assert.Equal(7, bufferWriter.Position);
            Assert.Equal((byte)0xC2, bufferWriter.CurrentSegment.Span[2]);
            Assert.Equal((byte)0xA3, bufferWriter.CurrentSegment.Span[3]);
            Assert.Equal((byte)0xC2, bufferWriter.CurrentSegment.Span[4]);
            Assert.Equal((byte)0xA3, bufferWriter.CurrentSegment.Span[5]);
            Assert.Equal((byte)'"', bufferWriter.CurrentSegment.Span[6]);

            textWriter.Write(']');
            textWriter.Flush();
            Assert.Equal(8, bufferWriter.Position);
            Assert.Equal((byte)']', bufferWriter.CurrentSegment.Span[7]);
        }

        [Fact]
        public void WriteChar_UnicodeLastChar()
        {
            var bufferWriter = new TestMemoryBufferWriter(4096);
            using (var textWriter = new Utf8BufferTextWriter())
            {
                textWriter.SetWriter(bufferWriter);

                textWriter.Write('\u00A3');
            }

            Assert.Equal(2, bufferWriter.Position);
            Assert.Equal((byte)0xC2, bufferWriter.CurrentSegment.Span[0]);
            Assert.Equal((byte)0xA3, bufferWriter.CurrentSegment.Span[1]);
        }

        [Fact]
        public void WriteChar_UnicodeAndRunOutOfBufferSpace()
        {
            var bufferWriter = new TestMemoryBufferWriter(4096);
            var textWriter = new Utf8BufferTextWriter();
            textWriter.SetWriter(bufferWriter);

            textWriter.Write('[');
            textWriter.Flush();
            Assert.Equal(1, bufferWriter.Position);
            Assert.Equal((byte)'[', bufferWriter.CurrentSegment.Span[0]);

            textWriter.Write('"');
            textWriter.Flush();
            Assert.Equal(2, bufferWriter.Position);
            Assert.Equal((byte)'"', bufferWriter.CurrentSegment.Span[1]);

            for (var i = 0; i < 2000; i++)
            {
                textWriter.Write('\u00A3');
            }
            textWriter.Flush();

            textWriter.Write('"');
            textWriter.Flush();
            Assert.Equal(4003, bufferWriter.Position);
            Assert.Equal((byte)'"', bufferWriter.CurrentSegment.Span[4002]);

            textWriter.Write(']');
            textWriter.Flush();
            Assert.Equal(4004, bufferWriter.Position);

            var result = Encoding.UTF8.GetString(bufferWriter.CurrentSegment.Slice(0, bufferWriter.Position).ToArray());
            Assert.Equal(2004, result.Length);

            Assert.Equal('[', result[0]);
            Assert.Equal('"', result[1]);

            for (var i = 0; i < 2000; i++)
            {
                Assert.Equal('\u00A3', result[i + 2]);
            }

            Assert.Equal('"', result[2002]);
            Assert.Equal(']', result[2003]);
        }

        [Fact]
        public void WriteCharArray_SurrogatePairInMultipleCalls()
        {
            var fourCircles = char.ConvertFromUtf32(0x1F01C);

            var chars = fourCircles.ToCharArray();

            var bufferWriter = new TestMemoryBufferWriter(4096);
            var textWriter = new Utf8BufferTextWriter();
            textWriter.SetWriter(bufferWriter);

            textWriter.Write(chars, 0, 1);
            textWriter.Flush();

            // Surrogate buffered
            Assert.Equal(0, bufferWriter.Position);

            textWriter.Write(chars, 1, 1);
            textWriter.Flush();

            Assert.Equal(4, bufferWriter.Position);

            var expectedData = Encoding.UTF8.GetBytes(fourCircles);

            var actualData = bufferWriter.CurrentSegment.Slice(0, 4).ToArray();

            Assert.Equal(expectedData, actualData);
        }

        [Fact]
        public void WriteChar_SurrogatePairInMultipleCalls()
        {
            var fourCircles = char.ConvertFromUtf32(0x1F01C);

            var chars = fourCircles.ToCharArray();

            var bufferWriter = new TestMemoryBufferWriter(4096);
            var textWriter = new Utf8BufferTextWriter();
            textWriter.SetWriter(bufferWriter);

            textWriter.Write(chars[0]);
            textWriter.Flush();

            // Surrogate buffered
            Assert.Equal(0, bufferWriter.Position);

            textWriter.Write(chars[1]);
            textWriter.Flush();

            Assert.Equal(4, bufferWriter.Position);

            var expectedData = Encoding.UTF8.GetBytes(fourCircles);

            var actualData = bufferWriter.CurrentSegment.Slice(0, 4).ToArray();

            Assert.Equal(expectedData, actualData);
        }

        [Fact]
        public void WriteCharArray_NonZeroStart()
        {
            var bufferWriter = new TestMemoryBufferWriter(4096);
            var textWriter = new Utf8BufferTextWriter();
            textWriter.SetWriter(bufferWriter);

            var chars = "Hello world".ToCharArray();

            textWriter.Write(chars, 6, 1);
            textWriter.Flush();

            Assert.Equal(1, bufferWriter.Position);
            Assert.Equal((byte)'w', bufferWriter.CurrentSegment.Span[0]);
        }

        [Fact]
        public void WriteCharArray_AcrossMultipleBuffers()
        {
            var bufferWriter = new TestMemoryBufferWriter(2);
            var textWriter = new Utf8BufferTextWriter();
            textWriter.SetWriter(bufferWriter);

            var chars = "Hello world".ToCharArray();

            textWriter.Write(chars);
            textWriter.Flush();

            var segments = bufferWriter.GetSegments();
            Assert.Equal(6, segments.Count);
            Assert.Equal(1, bufferWriter.Position);

            Assert.Equal((byte)'H', segments[0].Span[0]);
            Assert.Equal((byte)'e', segments[0].Span[1]);
            Assert.Equal((byte)'l', segments[1].Span[0]);
            Assert.Equal((byte)'l', segments[1].Span[1]);
            Assert.Equal((byte)'o', segments[2].Span[0]);
            Assert.Equal((byte)' ', segments[2].Span[1]);
            Assert.Equal((byte)'w', segments[3].Span[0]);
            Assert.Equal((byte)'o', segments[3].Span[1]);
            Assert.Equal((byte)'r', segments[4].Span[0]);
            Assert.Equal((byte)'l', segments[4].Span[1]);
            Assert.Equal((byte)'d', segments[5].Span[0]);
        }

        [Fact]
        public void GetAndReturnCachedBufferTextWriter()
        {
            var bufferWriter1 = new TestMemoryBufferWriter();

            var textWriter1 = Utf8BufferTextWriter.Get(bufferWriter1);
            textWriter1.Write("Hello");
            textWriter1.Flush();
            Utf8BufferTextWriter.Return(textWriter1);

            Assert.Equal("Hello", Encoding.UTF8.GetString(bufferWriter1.ToArray()));

            var bufferWriter2 = new TestMemoryBufferWriter();

            var textWriter2 = Utf8BufferTextWriter.Get(bufferWriter2);
            textWriter2.Write("World");
            textWriter2.Flush();
            Utf8BufferTextWriter.Return(textWriter2);

            Assert.Equal("World", Encoding.UTF8.GetString(bufferWriter2.ToArray()));

            Assert.Same(textWriter1, textWriter2);
        }

        [Fact]
        public void WriteMultiByteCharactersToSmallBuffers()
        {
            // Test string breakdown (char => UTF-8 hex values):
            // a => 61
            // い => E3-81-84
            // b => 62
            // ろ => E3-82-8D
            // c => 63
            // d => 64
            // は => E3-81-AF
            // に => E3-81-AB
            // e => 65
            // ほ => E3-81-BB
            // f => 66
            // へ => E3-81-B8
            // ど => E3-81-A9
            // g => 67
            // h => 68
            // i => 69
            // \uD800\uDC00 => F0-90-80-80 (this is a surrogate pair that is represented as a single 4-byte UTF-8 encoding)
            const string testString = "aいbろcdはにeほfへどghi\uD800\uDC00";

            // By mixing single byte and multi-byte characters, we know that there will
            // be spaces in the active segment that cannot fit the current character. This
            // means we'll be testing the GetMemory(minimumSize) logic.
            var bufferWriter = new TestMemoryBufferWriter(segmentSize: 5);

            var writer = new Utf8BufferTextWriter();
            writer.SetWriter(bufferWriter);
            writer.Write(testString);
            writer.Flush();

            // Verify the output
            var allSegments = bufferWriter.GetSegments().Select(s => s.ToArray()).ToArray();
            Assert.Collection(allSegments,
                seg => Assert.Equal(new byte[] { 0x61, 0xE3, 0x81, 0x84, 0x62 }, seg),  // "aいb"
                seg => Assert.Equal(new byte[] { 0xE3, 0x82, 0x8D, 0x63, 0x64 }, seg),  // "ろcd"
                seg => Assert.Equal(new byte[] { 0xE3, 0x81, 0xAF }, seg),              // "は"
                seg => Assert.Equal(new byte[] { 0xE3, 0x81, 0xAB, 0x65 }, seg),        // "にe"
                seg => Assert.Equal(new byte[] { 0xE3, 0x81, 0xBB, 0x66 }, seg),        // "ほf"
                seg => Assert.Equal(new byte[] { 0xE3, 0x81, 0xB8 }, seg),              // "へ"
                seg => Assert.Equal(new byte[] { 0xE3, 0x81, 0xA9, 0x67, 0x68 }, seg),        // "どgh"
                seg => Assert.Equal(new byte[] { 0x69, 0xF0, 0x90, 0x80, 0x80 }, seg));       // "i\uD800\uDC00"

            Assert.Equal(testString, Encoding.UTF8.GetString(bufferWriter.ToArray()));
        }

        public static IEnumerable<object[]> CharAndSegmentSizes
        {
            get
            {
                foreach (var singleChar in new [] { '"', 'い' })
                {
                    for (int i = 4; i <= 16; i++)
                    {
                        yield return new object[] { singleChar, i };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(CharAndSegmentSizes))]
        public void WriteUnicodeStringAndCharsWithVaryingSegmentSizes(char singleChar, int segmentSize)
        {
            const string testString = "aいbろ";
            const int iterations = 10;

            var testBufferWriter = new TestMemoryBufferWriter(segmentSize);
            var sb = new StringBuilder();

            using (var textWriter = new Utf8BufferTextWriter())
            {
                textWriter.SetWriter(testBufferWriter);

                for (int i = 0; i < iterations; i++)
                {
                    textWriter.Write(singleChar);
                    textWriter.Write(testString);
                    textWriter.Write(singleChar);

                    sb.Append(singleChar);
                    sb.Append(testString);
                    sb.Append(singleChar);
                }
            }

            var expected = sb.ToString();

            var data = testBufferWriter.ToArray();
            var result = Encoding.UTF8.GetString(data);

            Assert.Equal(expected, result);
        }

        private sealed class TestMemoryBufferWriter : IBufferWriter<byte>
        {
            private readonly int _segmentSize;

            private readonly List<Memory<byte>> _completedSegments = new List<Memory<byte>>();
            private int _totalLength;

            public Memory<byte> CurrentSegment { get; private set; }
            internal int Position { get; private set; }

            public TestMemoryBufferWriter(int segmentSize = 2048)
            {
                _segmentSize = segmentSize;
                CurrentSegment = Memory<byte>.Empty;
            }

            public void Advance(int count)
            {
                Position += count;
                _totalLength += count;
            }

            public Memory<byte> GetMemory(int sizeHint = 0)
            {
                // Need special handling for sizeHint == 0, because for that we want to enter the if even if there are "sizeHint" (i.e. 0) bytes left :).
                if ((sizeHint == 0 && CurrentSegment.Length == Position) || (CurrentSegment.Length - Position < sizeHint))
                {
                    if (Position > 0)
                    {
                        // Complete the current segment
                        _completedSegments.Add(CurrentSegment.Slice(0, Position));
                    }

                    // Allocate a new segment and reset the position.
                    CurrentSegment = new Memory<byte>(new byte[_segmentSize]);
                    Position = 0;
                }

                return CurrentSegment.Slice(Position, CurrentSegment.Length - Position);
            }

            public Span<byte> GetSpan(int sizeHint = 0)
            {
                return GetMemory(sizeHint).Span;
            }

            public byte[] ToArray()
            {
                if (CurrentSegment.IsEmpty && _completedSegments.Count == 0)
                {
                    return Array.Empty<byte>();
                }

                var result = new byte[_totalLength];

                var totalWritten = 0;

                // Copy completed segments
                foreach (var segment in _completedSegments)
                {
                    segment.CopyTo(result.AsMemory(totalWritten, segment.Length));

                    totalWritten += segment.Length;
                }

                // Copy current segment
                CurrentSegment.Slice(0, Position).CopyTo(result.AsMemory(totalWritten, Position));

                return result;
            }

            public IList<Memory<byte>> GetSegments()
            {
                var list = new List<Memory<byte>>();
                foreach (var segment in _completedSegments)
                {
                    list.Add(segment);
                }

                if (CurrentSegment.Length > 0)
                {
                    list.Add(CurrentSegment.Slice(0, Position));
                }

                return list;
            }
        }
    }
}
