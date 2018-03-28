// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol
{
    public class Utf8BufferTextWriterTests
    {
        [Fact]
        public void WriteChar_Unicode()
        {
            MemoryBufferWriter bufferWriter = new MemoryBufferWriter(4096);
            Utf8BufferTextWriter textWriter = new Utf8BufferTextWriter();
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
            MemoryBufferWriter bufferWriter = new MemoryBufferWriter(4096);
            using (Utf8BufferTextWriter textWriter = new Utf8BufferTextWriter())
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
            MemoryBufferWriter bufferWriter = new MemoryBufferWriter(4096);
            Utf8BufferTextWriter textWriter = new Utf8BufferTextWriter();
            textWriter.SetWriter(bufferWriter);

            textWriter.Write('[');
            textWriter.Flush();
            Assert.Equal(1, bufferWriter.Position);
            Assert.Equal((byte)'[', bufferWriter.CurrentSegment.Span[0]);

            textWriter.Write('"');
            textWriter.Flush();
            Assert.Equal(2, bufferWriter.Position);
            Assert.Equal((byte)'"', bufferWriter.CurrentSegment.Span[1]);

            for (int i = 0; i < 2000; i++)
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

            string result = Encoding.UTF8.GetString(bufferWriter.CurrentSegment.Slice(0, bufferWriter.Position).ToArray());
            Assert.Equal(2004, result.Length);

            Assert.Equal('[', result[0]);
            Assert.Equal('"', result[1]);

            for (int i = 0; i < 2000; i++)
            {
                Assert.Equal('\u00A3', result[i + 2]);
            }

            Assert.Equal('"', result[2002]);
            Assert.Equal(']', result[2003]);
        }

        [Fact]
        public void WriteCharArray_SurrogatePairInMultipleCalls()
        {
            string fourCircles = char.ConvertFromUtf32(0x1F01C);

            char[] chars = fourCircles.ToCharArray();

            MemoryBufferWriter bufferWriter = new MemoryBufferWriter(4096);
            Utf8BufferTextWriter textWriter = new Utf8BufferTextWriter();
            textWriter.SetWriter(bufferWriter);

            textWriter.Write(chars, 0, 1);
            textWriter.Flush();

            // Surrogate buffered
            Assert.Equal(0, bufferWriter.Position);

            textWriter.Write(chars, 1, 1);
            textWriter.Flush();

            Assert.Equal(4, bufferWriter.Position);

            byte[] expectedData = Encoding.UTF8.GetBytes(fourCircles);

            byte[] actualData = bufferWriter.CurrentSegment.Slice(0, 4).ToArray();

            Assert.Equal(expectedData, actualData);
        }

        [Fact]
        public void WriteChar_SurrogatePairInMultipleCalls()
        {
            string fourCircles = char.ConvertFromUtf32(0x1F01C);

            char[] chars = fourCircles.ToCharArray();

            MemoryBufferWriter bufferWriter = new MemoryBufferWriter(4096);
            Utf8BufferTextWriter textWriter = new Utf8BufferTextWriter();
            textWriter.SetWriter(bufferWriter);

            textWriter.Write(chars[0]);
            textWriter.Flush();

            // Surrogate buffered
            Assert.Equal(0, bufferWriter.Position);

            textWriter.Write(chars[1]);
            textWriter.Flush();

            Assert.Equal(4, bufferWriter.Position);

            byte[] expectedData = Encoding.UTF8.GetBytes(fourCircles);

            byte[] actualData = bufferWriter.CurrentSegment.Slice(0, 4).ToArray();

            Assert.Equal(expectedData, actualData);
        }

        [Fact]
        public void WriteCharArray_NonZeroStart()
        {
            MemoryBufferWriter bufferWriter = new MemoryBufferWriter(4096);
            Utf8BufferTextWriter textWriter = new Utf8BufferTextWriter();
            textWriter.SetWriter(bufferWriter);

            char[] chars = "Hello world".ToCharArray();

            textWriter.Write(chars, 6, 1);
            textWriter.Flush();

            Assert.Equal(1, bufferWriter.Position);
            Assert.Equal((byte)'w', bufferWriter.CurrentSegment.Span[0]);
        }

        [Fact]
        public void WriteCharArray_AcrossMultipleBuffers()
        {
            MemoryBufferWriter bufferWriter = new MemoryBufferWriter(2);
            Utf8BufferTextWriter textWriter = new Utf8BufferTextWriter();
            textWriter.SetWriter(bufferWriter);

            char[] chars = "Hello world".ToCharArray();

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
            MemoryBufferWriter bufferWriter1 = new MemoryBufferWriter();

            var textWriter1 = Utf8BufferTextWriter.Get(bufferWriter1);
            textWriter1.Write("Hello");
            textWriter1.Flush();
            Utf8BufferTextWriter.Return(textWriter1);

            Assert.Equal("Hello", Encoding.UTF8.GetString(bufferWriter1.ToArray()));

            MemoryBufferWriter bufferWriter2 = new MemoryBufferWriter();

            var textWriter2 = Utf8BufferTextWriter.Get(bufferWriter2);
            textWriter2.Write("World");
            textWriter2.Flush();
            Utf8BufferTextWriter.Return(textWriter2);

            Assert.Equal("World", Encoding.UTF8.GetString(bufferWriter2.ToArray()));

            Assert.Same(textWriter1, textWriter2);
        }
    }
}
