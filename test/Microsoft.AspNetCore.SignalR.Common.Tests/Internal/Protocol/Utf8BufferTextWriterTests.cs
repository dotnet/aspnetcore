// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
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

            Assert.Equal(6, bufferWriter.Segments.Count);
            Assert.Equal(1, bufferWriter.Position);

            Assert.Equal((byte)'H', bufferWriter.Segments[0].Span[0]);
            Assert.Equal((byte)'e', bufferWriter.Segments[0].Span[1]);
            Assert.Equal((byte)'l', bufferWriter.Segments[1].Span[0]);
            Assert.Equal((byte)'l', bufferWriter.Segments[1].Span[1]);
            Assert.Equal((byte)'o', bufferWriter.Segments[2].Span[0]);
            Assert.Equal((byte)' ', bufferWriter.Segments[2].Span[1]);
            Assert.Equal((byte)'w', bufferWriter.Segments[3].Span[0]);
            Assert.Equal((byte)'o', bufferWriter.Segments[3].Span[1]);
            Assert.Equal((byte)'r', bufferWriter.Segments[4].Span[0]);
            Assert.Equal((byte)'l', bufferWriter.Segments[4].Span[1]);
            Assert.Equal((byte)'d', bufferWriter.Segments[5].Span[0]);
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

        private sealed class TestMemoryBufferWriter : IBufferWriter<byte>
        {
            private readonly int _segmentSize;

            internal List<Memory<byte>> Segments { get; }
            internal int Position { get; private set; }

            public TestMemoryBufferWriter(int segmentSize = 2048)
            {
                _segmentSize = segmentSize;

                Segments = new List<Memory<byte>>();
            }

            public Memory<byte> CurrentSegment => Segments.Count > 0 ? Segments[Segments.Count - 1] : null;

            public void Advance(int count)
            {
                Position += count;
            }

            public Memory<byte> GetMemory(int sizeHint = 0)
            {
                // TODO: Use sizeHint

                if (Segments.Count == 0 || Position == _segmentSize)
                {
                    // TODO: Rent memory from a pool
                    Segments.Add(new Memory<byte>(new byte[_segmentSize]));
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
                if (Segments.Count == 0)
                {
                    return Array.Empty<byte>();
                }

                var totalLength = (Segments.Count - 1) * _segmentSize;
                totalLength += Position;

                var result = new byte[totalLength];

                var totalWritten = 0;

                // Copy full segments
                for (var i = 0; i < Segments.Count - 1; i++)
                {
                    Segments[i].CopyTo(result.AsMemory(totalWritten, _segmentSize));

                    totalWritten += _segmentSize;
                }

                // Copy current incomplete segment
                CurrentSegment.Slice(0, Position).CopyTo(result.AsMemory(totalWritten, Position));

                return result;
            }
        }
    }
}
