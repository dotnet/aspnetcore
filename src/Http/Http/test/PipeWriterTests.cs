// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Http.Tests
{
    public class PipeWriterTests : PipeTest
    {

        [Theory]
        [InlineData(3, -1, 0)]
        [InlineData(3, 0, -1)]
        [InlineData(3, 0, 4)]
        [InlineData(3, 4, 0)]
        [InlineData(3, -1, -1)]
        [InlineData(3, 4, 4)]
        public void ThrowsForInvalidParameters(int arrayLength, int offset, int length)
        {
            var array = new byte[arrayLength];
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = (byte)(i + 1);
            }

            Writer.Write(new Span<byte>(array, 0, 0));
            Writer.Write(new Span<byte>(array, array.Length, 0));

            try
            {
                Writer.Write(new Span<byte>(array, offset, length));
                Assert.True(false);
            }
            catch (Exception ex)
            {
                Assert.True(ex is ArgumentOutOfRangeException);
            }

            Writer.Write(new Span<byte>(array, 0, array.Length));
            Assert.Equal(array, Read());
        }

        [Theory]
        [InlineData(0, 3)]
        [InlineData(1, 2)]
        [InlineData(2, 1)]
        [InlineData(1, 1)]
        public void CanWriteWithOffsetAndLength(int offset, int length)
        {
            var array = new byte[] { 1, 2, 3 };

            Writer.Write(new Span<byte>(array, offset, length));

            Assert.Equal(array.Skip(offset).Take(length).ToArray(), Read());
        }

        [Fact]
        public void CanWriteIntoHeadlessBuffer()
        {

            Writer.Write(new byte[] { 1, 2, 3 });
            Assert.Equal(new byte[] { 1, 2, 3 }, Read());
        }

        [Fact]
        public void CanGetNewMemoryWhenSizeTooLarge()
        {
            var memory = Writer.GetMemory(0);

            var memoryLarge = Writer.GetMemory(10000);

            Assert.NotEqual(memory, memoryLarge);
        }

        [Fact]
        public void CanGetSameMemoryWhenNoAdvance()
        {
            var memory = Writer.GetMemory(0);

            var secondMemory = Writer.GetMemory(0);

            Assert.Equal(memory, secondMemory);
        }

        [Fact]
        public void CanGetNewSpanWhenNoAdvanceWhenSizeTooLarge()
        {
            var span = Writer.GetSpan(0);

            var secondSpan = Writer.GetSpan(10000);

            Assert.False(span.SequenceEqual(secondSpan));
        }

        [Fact]
        public void CanGetSameSpanWhenNoAdvance()
        {
            var span = Writer.GetSpan(0);

            var secondSpan = Writer.GetSpan(0);

            Assert.True(span.SequenceEqual(secondSpan));
        }

        [Theory]
        [InlineData(16, 32, 32)]
        [InlineData(16, 16, 16)]
        [InlineData(64, 32, 64)]
        [InlineData(40, 32, 64)] // memory sizes are powers of 2.
        public void CheckMinimumSegmentSizeWithGetMemory(int minimumSegmentSize, int getMemorySize, int expectedSize)
        {
            var writer = new StreamPipeWriter(new MemoryStream(), minimumSegmentSize);
            var memory = writer.GetMemory(getMemorySize);

            Assert.Equal(expectedSize, memory.Length);
        }

        [Fact]
        public void CanWriteMultipleTimes()
        {

            Writer.Write(new byte[] { 1 });
            Writer.Write(new byte[] { 2 });
            Writer.Write(new byte[] { 3 });

            Assert.Equal(new byte[] { 1, 2, 3 }, Read());
        }

        [Fact]
        public void CanWriteOverTheBlockLength()
        {
            Memory<byte> memory = Writer.GetMemory();

            IEnumerable<byte> source = Enumerable.Range(0, memory.Length).Select(i => (byte)i);
            byte[] expectedBytes = source.Concat(source).Concat(source).ToArray();

            Writer.Write(expectedBytes);

            Assert.Equal(expectedBytes, Read());
        }

        [Fact]
        public void EnsureAllocatesSpan()
        {
            var span = Writer.GetSpan(10);

            Assert.True(span.Length >= 10);
            // 0 byte Flush would not complete the reader so we complete.
            Writer.Complete();
            Assert.Equal(new byte[] { }, Read());
        }

        [Fact]
        public void SlicesSpanAndAdvancesAfterWrite()
        {
            int initialLength = Writer.GetSpan(3).Length;


            Writer.Write(new byte[] { 1, 2, 3 });
            Span<byte> span = Writer.GetSpan();

            Assert.Equal(initialLength - 3, span.Length);
            Assert.Equal(new byte[] { 1, 2, 3 }, Read());
        }

        [Theory]
        [InlineData(5)]
        [InlineData(50)]
        [InlineData(500)]
        [InlineData(5000)]
        [InlineData(50000)]
        public async Task WriteLargeDataBinary(int length)
        {
            var data = new byte[length];
            new Random(length).NextBytes(data);
            PipeWriter output = Writer;
            output.Write(data);
            await output.FlushAsync();

            var input = Read();
            Assert.Equal(data, input.ToArray());
        }

        [Fact]
        public async Task CanWriteNothingToBuffer()
        {
            Writer.GetMemory(0);
            Writer.Advance(0); // doing nothing, the hard way
            await Writer.FlushAsync();
        }

        [Fact]
        public void EmptyWriteDoesNotThrow()
        {
            Writer.Write(new byte[0]);
        }

        [Fact]
        public void ThrowsOnAdvanceOverMemorySize()
        {
            Memory<byte> buffer = Writer.GetMemory(1);
            var exception = Assert.Throws<InvalidOperationException>(() => Writer.Advance(buffer.Length + 1));
            Assert.Equal("Can't advance past buffer size.", exception.Message);
        }

        [Fact]
        public void ThrowsOnAdvanceWithNoMemory()
        {
            PipeWriter buffer = Writer;
            var exception = Assert.Throws<InvalidOperationException>(() => buffer.Advance(1));
            Assert.Equal("No writing operation. Make sure GetMemory() was called.", exception.Message);
        }
    }
}
