// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Net.Http;
using Xunit;

namespace Common.Tests.Tests.System.Net.aspnetcore.Http3
{
    public class VariableLengthIntegerHelperTests
    {
        [Fact]
        public void TryRead_FromReadOnlySpan_BufferEmpty()
        {
            ReadOnlySpan<byte> readOnlySpan = new ReadOnlySpan<byte>();
            bool isSuccess = VariableLengthIntegerHelper.TryRead(readOnlySpan,
                out long value, out int bytesRead);

            Assert.False(isSuccess);
            Assert.Equal(0, value);
            Assert.Equal(0, bytesRead);
        }

        [Fact]
        public void TryRead_FromReadOnlySpan_BufferNotEmpty_InitialOneByteLengthMask()
        {
            ReadOnlySpan<byte> readOnlySpan = new ReadOnlySpan<byte>(new byte[]
            {
                1
            });
            bool isSuccess = VariableLengthIntegerHelper.TryRead(readOnlySpan,
                out long value, out int bytesRead);

            Assert.True(isSuccess);
            Assert.Equal(1, value);
            Assert.Equal(1, bytesRead);
        }

        [Fact]
        public void TryRead_FromReadOnlySpan_BufferNotEmpty_InitialTwoByteLengthMask_Buffer16BigEndian()
        {
            ReadOnlySpan<byte> readOnlySpan = new ReadOnlySpan<byte>(new byte[]
            {
                64,
                1
            });
            bool isSuccess = VariableLengthIntegerHelper.TryRead(readOnlySpan,
                out long value, out int bytesRead);

            Assert.True(isSuccess);
            Assert.Equal(1, value);
            Assert.Equal(2, bytesRead);
        }

        [Fact]
        public void TryRead_FromReadOnlySpan_BufferNotEmpty_InitialTwoByteLengthMask_BufferNot16BigEndian()
        {
            ReadOnlySpan<byte> readOnlySpan = new ReadOnlySpan<byte>(new byte[]
            {
                64
            });
            bool isSuccess = VariableLengthIntegerHelper.TryRead(readOnlySpan,
                out long value, out int bytesRead);

            Assert.False(isSuccess);
            Assert.Equal(0, value);
            Assert.Equal(0, bytesRead);
        }

        [Fact]
        public void TryRead_FromReadOnlySpan_BufferNotEmpty_InitialFourByteLengthMask_TryReadUInt32BigEndian()
        {
            ReadOnlySpan<byte> readOnlySpan = new ReadOnlySpan<byte>(new byte[]
            {
                128,
                0,
                0,
                2
            });
            bool isSuccess = VariableLengthIntegerHelper.TryRead(readOnlySpan,
                out long value, out int bytesRead);

            Assert.True(isSuccess);
            Assert.Equal(2, value);
            Assert.Equal(4, bytesRead);
        }

        [Fact]
        public void TryRead_FromReadOnlySpan_BufferNotEmpty_InitialFourByteLengthMask_TryReadNotUInt32BigEndian()
        {
            ReadOnlySpan<byte> readOnlySpan = new ReadOnlySpan<byte>(new byte[]
            {
                128
            });
            bool isSuccess = VariableLengthIntegerHelper.TryRead(readOnlySpan,
                out long value, out int bytesRead);

            Assert.False(isSuccess);
            Assert.Equal(0, value);
            Assert.Equal(0, bytesRead);
        }

        [Fact]
        public void TryRead_FromReadOnlySpan_BufferNotEmpty_InitialEightByteLengthMask_TryReadUInt64BigEndian()
        {
            ReadOnlySpan<byte> readOnlySpan = new ReadOnlySpan<byte>(
                new byte[]
            {
                192, 0, 0, 0,
                0, 0, 0, 4
            });
            bool isSuccess = VariableLengthIntegerHelper.TryRead(readOnlySpan,
                out long value, out int bytesRead);

            Assert.True(isSuccess);
            Assert.Equal(4, value);
            Assert.Equal(8, bytesRead);
        }

        [Fact]
        public void TryRead_FromReadOnlySpan_BufferNotEmpty_InitialEightByteLengthMask_TryReadNotUInt64BigEndian()
        {
            ReadOnlySpan<byte> readOnlySpan = new ReadOnlySpan<byte>(new byte[]
            {
                192
            });
            bool isSuccess = VariableLengthIntegerHelper.TryRead(readOnlySpan,
                out long value, out int bytesRead);

            Assert.False(isSuccess);
            Assert.Equal(0, value);
            Assert.Equal(0, bytesRead);
        }

        [Fact]
        public void TryRead_FromSequenceReader_NotSegmentedSequence()
        {
            ReadOnlySequence<byte> readOnlySequence = new ReadOnlySequence<byte>(new byte[]
            {
                1
            });
            SequenceReader<byte> sequenceReader = new SequenceReader<byte>(readOnlySequence);
            bool isSuccess = VariableLengthIntegerHelper.TryRead(ref sequenceReader,
                out long value);

            Assert.True(isSuccess);
            Assert.Equal(1, value);
            Assert.Equal(1, sequenceReader.CurrentSpanIndex);
        }

        internal class MemorySegment<T> : ReadOnlySequenceSegment<T>
        {
            internal MemorySegment(ReadOnlyMemory<T> memory)
            {
                Memory = memory;
            }

            internal MemorySegment<T> Append(ReadOnlyMemory<T> memory)
            {
                var segment = new MemorySegment<T>(memory)
                {
                    RunningIndex = RunningIndex + Memory.Length
                };

                Next = segment;

                return segment;
            }
        }

        [Fact]
        public void TryRead_FromSequenceReader_InitialTwoByteLengthMask_SegmentedSequence()
        {
            MemorySegment<byte> memorySegment1 = new MemorySegment<byte>(new byte[] { 64 });
            MemorySegment<byte> memorySegment2 = memorySegment1.Append(new byte[] { 1 });
            ReadOnlySequence<byte> readOnlySequence = new ReadOnlySequence<byte>(
                memorySegment1, 0, memorySegment2, memorySegment2.Memory.Length);
            SequenceReader<byte> sequenceReader = new SequenceReader<byte>(readOnlySequence);
            bool isSuccess = VariableLengthIntegerHelper.TryRead(ref sequenceReader,
                out long value);

            Assert.True(isSuccess);
            Assert.Equal(1, value);
            Assert.Equal(1, sequenceReader.CurrentSpanIndex);
        }

        [Fact]
        public void TryRead_FromSequenceReader_InitialFourByteLengthMask_SegmentedSequence()
        {
            MemorySegment<byte> memorySegment1 = new MemorySegment<byte>(new byte[] { 192 });
            MemorySegment<byte> memorySegment2 = memorySegment1.Append(new byte[] { 0, 0, 0, 0, 0, 0, 2 });
            ReadOnlySequence<byte> readOnlySequence = new ReadOnlySequence<byte>(
                memorySegment1, 0, memorySegment2, memorySegment2.Memory.Length);
            SequenceReader<byte> sequenceReader = new SequenceReader<byte>(readOnlySequence);
            bool isSuccess = VariableLengthIntegerHelper.TryRead(ref sequenceReader,
                out long value);

            Assert.True(isSuccess);
            Assert.Equal(2, value);
            Assert.Equal(7, sequenceReader.CurrentSpanIndex);
        }

        [Fact]
        public void TryRead_FromSequenceReader_NotValidSegmentedSequence()
        {
            MemorySegment<byte> memorySegment1 = new MemorySegment<byte>(new byte[] { 192 });
            MemorySegment<byte> memorySegment2 = memorySegment1.Append(new byte[] { 0, 0, 0, 0, 0, 2 });
            ReadOnlySequence<byte> readOnlySequence = new ReadOnlySequence<byte>(
                memorySegment1, 0, memorySegment2, memorySegment2.Memory.Length);
            SequenceReader<byte> sequenceReader = new SequenceReader<byte>(readOnlySequence);
            bool isSuccess = VariableLengthIntegerHelper.TryRead(ref sequenceReader,
                out long value);

            Assert.False(isSuccess);
            Assert.Equal(0, value);
        }

        [Fact]
        public void GetInteger_ValidSegmentedSequence()
        {
            MemorySegment<byte> memorySegment1 = new MemorySegment<byte>(new byte[] { 192 });
            MemorySegment<byte> memorySegment2 = memorySegment1.Append(new byte[] { 0, 0, 0, 0, 0, 0, 2 });
            ReadOnlySequence<byte> readOnlySequence = new ReadOnlySequence<byte>(
                memorySegment1, 0, memorySegment2, memorySegment2.Memory.Length);
            long result = VariableLengthIntegerHelper.GetInteger(readOnlySequence,
                out SequencePosition consumed, out SequencePosition examined);

            Assert.Equal(2, result);
            Assert.Equal(7, consumed.GetInteger());
            Assert.Equal(7, examined.GetInteger());
        }

        [Fact]
        public void GetInteger_NotValidSegmentedSequence()
        {
            MemorySegment<byte> memorySegment1 = new MemorySegment<byte>(new byte[] { 192 });
            MemorySegment<byte> memorySegment2 = memorySegment1.Append(new byte[] { 0, 0, 0, 0, 0, 2 });
            ReadOnlySequence<byte> readOnlySequence = new ReadOnlySequence<byte>(
                memorySegment1, 0, memorySegment2, memorySegment2.Memory.Length);
            long result = VariableLengthIntegerHelper.GetInteger(readOnlySequence,
                out SequencePosition consumed, out SequencePosition examined);

            Assert.Equal(-1, result);
            Assert.Equal(0, consumed.GetInteger());
            Assert.Equal(6, examined.GetInteger());
        }

        [Fact]
        public void TryWrite_BufferEmpty()
        {
            Span<byte> span = new Span<byte>();
            long longToEncode = 1;
            bool isSuccess = VariableLengthIntegerHelper.TryWrite(span,
                longToEncode, out int bytesWritten);

            Assert.False(isSuccess);
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(63)]
        public void TryWrite_BufferNotEmpty_OneByteLimit(long longToEncode)
        {
            Span<byte> span = new Span<byte>(new byte[1]);
            bool isSuccess = VariableLengthIntegerHelper.TryWrite(span,
                longToEncode, out int bytesWritten);

            Assert.True(isSuccess);
            Assert.Equal(1, bytesWritten);
            Assert.Equal(longToEncode, span[0]);
        }

        [Theory]
        [InlineData(64, new byte[] { 64, 64 })]
        [InlineData(66, new byte[] { 64, 66 })]
        [InlineData(16383, new byte[] { 127, 255 })]
        public void TryWrite_BufferNotEmpty_TwoByteLimit(long longToEncode,
            byte[] expected)
        {
            Span<byte> span = new Span<byte>(new byte[2]);
            bool isSuccess = VariableLengthIntegerHelper.TryWrite(span,
                longToEncode, out int bytesWritten);

            Assert.True(isSuccess);
            Assert.Equal(2, bytesWritten);
            Assert.Equal(expected, span.ToArray());
        }

        [Fact]
        public void TryWrite_BufferNotSizedCorrectly_TwoByteLimit()
        {
            long longToEncode = 64;
            Span<byte> span = new Span<byte>(new byte[1]);
            bool isSuccess = VariableLengthIntegerHelper.TryWrite(span,
                longToEncode, out int bytesWritten);

            Assert.False(isSuccess);
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [InlineData(16384, new byte[] {128, 0, 64, 0 })]
        [InlineData(16386, new byte[] { 128, 0, 64, 2 })]
        [InlineData(1073741823, new byte[] { 191, 255, 255, 255 })]
        public void TryWrite_BufferNotEmpty_FourByteLimit(long longToEncode,
            byte[] expected)
        {
            Span<byte> span = new Span<byte>(new byte[4]);
            bool isSuccess = VariableLengthIntegerHelper.TryWrite(span,
                longToEncode, out int bytesWritten);

            Assert.True(isSuccess);
            Assert.Equal(4, bytesWritten);
            Assert.Equal(expected, span.ToArray());
        }

        [Fact]
        public void TryWrite_BufferNotSizedCorrectly_FourByteLimit()
        {
            long longToEncode = 16384;
            Span<byte> span = new Span<byte>(new byte[1]);
            bool isSuccess = VariableLengthIntegerHelper.TryWrite(span,
                longToEncode, out int bytesWritten);

            Assert.False(isSuccess);
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [InlineData(1073741824, new byte[] { 192, 0, 0, 0, 64, 0, 0, 0 })]
        [InlineData(1073741826, new byte[] { 192, 0, 0, 0, 64, 0, 0, 2 })]
        [InlineData(4611686018427387903, new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 })]
        public void TryWrite_BufferNotEmpty_EightByteLimit(long longToEncode,
            byte[] expected)
        {
            Span<byte> span = new Span<byte>(new byte[8]);
            bool isSuccess = VariableLengthIntegerHelper.TryWrite(span,
                longToEncode, out int bytesWritten);

            Assert.True(isSuccess);
            Assert.Equal(8, bytesWritten);
            Assert.Equal(expected, span.ToArray());
        }

        [Fact]
        public void TryWrite_BufferNotSizedCorrectly_EightByteLimit()
        {
            long longToEncode = 1073741824;
            Span<byte> span = new Span<byte>(new byte[1]);
            bool isSuccess = VariableLengthIntegerHelper.TryWrite(span,
                longToEncode, out int bytesWritten);

            Assert.False(isSuccess);
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(64, 2)]
        [InlineData(16384, 4)]
        [InlineData(1073741824, 8)]
        public void GetByteCountTest(long longToEncode, int expectedLimit)
        {
            int result = VariableLengthIntegerHelper.GetByteCount(longToEncode);

            Assert.Equal(expectedLimit, result);
        }
    }
}
