// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Internal;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol
{
    public class MemoryBufferWriterTests
    {
        private static int MinimumSegmentSize;

        static MemoryBufferWriterTests()
        {
            var buffer = ArrayPool<byte>.Shared.Rent(1);
            // Compute the minimum segment size of the array pool
            MinimumSegmentSize = buffer.Length;
            ArrayPool<byte>.Shared.Return(buffer);
        }

        [Fact]
        public void WritingNotingGivesEmptyData()
        {
            using (var bufferWriter = new MemoryBufferWriter())
            {
                Assert.Equal(0, bufferWriter.Length);
                var data = bufferWriter.ToArray();
                Assert.Empty(data);
            }
        }

        [Fact]
        public void WritingNotingGivesEmptyData_CopyTo()
        {
            using (var bufferWriter = new MemoryBufferWriter())
            {
                Assert.Equal(0, bufferWriter.Length);
                var data = new byte[bufferWriter.Length];
                bufferWriter.CopyTo(data);
                Assert.Empty(data);
            }
        }

        [Fact]
        public void WriteByteWorksAsFirstCall()
        {
            using (var bufferWriter = new MemoryBufferWriter())
            {
                bufferWriter.WriteByte(234);
                var data = bufferWriter.ToArray();

                Assert.Equal(1, bufferWriter.Length);
                Assert.Single(data);
                Assert.Equal(234, data[0]);
            }
        }

        [Fact]
        public void WriteByteWorksAsFirstCall_CopyTo()
        {
            using (var bufferWriter = new MemoryBufferWriter())
            {
                bufferWriter.WriteByte(234);

                Assert.Equal(1, bufferWriter.Length);
                var data = new byte[bufferWriter.Length];

                bufferWriter.CopyTo(data);
                Assert.Equal(234, data[0]);
            }
        }

        [Fact]
        public void WriteByteWorksIfFirstByteInNewSegment()
        {
            var inputSize = MinimumSegmentSize;
            var input = Enumerable.Range(0, inputSize).Select(i => (byte)i).ToArray();

            using (var bufferWriter = new MemoryBufferWriter(MinimumSegmentSize))
            {
                bufferWriter.Write(input, 0, input.Length);
                Assert.Equal(16, bufferWriter.Length);
                bufferWriter.WriteByte(16);
                Assert.Equal(17, bufferWriter.Length);

                var data = bufferWriter.ToArray();
                Assert.Equal(input, data.Take(16));
                Assert.Equal(16, data[16]);
            }
        }

        [Fact]
        public void WriteByteWorksIfFirstByteInNewSegment_CopyTo()
        {
            var inputSize = MinimumSegmentSize;
            var input = Enumerable.Range(0, inputSize).Select(i => (byte)i).ToArray();

            using (var bufferWriter = new MemoryBufferWriter(MinimumSegmentSize))
            {
                bufferWriter.Write(input, 0, input.Length);
                Assert.Equal(16, bufferWriter.Length);
                bufferWriter.WriteByte(16);
                Assert.Equal(17, bufferWriter.Length);

                var data = new byte[bufferWriter.Length];

                bufferWriter.CopyTo(data);
                Assert.Equal(input, data.Take(16));
                Assert.Equal(16, data[16]);
            }
        }

        [Fact]
        public void WriteByteWorksIfSegmentHasSpace()
        {
            var input = new byte[] { 11, 12, 13 };

            using (var bufferWriter = new MemoryBufferWriter())
            {
                bufferWriter.Write(input, 0, input.Length);
                bufferWriter.WriteByte(14);

                Assert.Equal(4, bufferWriter.Length);

                var data = bufferWriter.ToArray();
                Assert.Equal(4, data.Length);
                Assert.Equal(11, data[0]);
                Assert.Equal(12, data[1]);
                Assert.Equal(13, data[2]);
                Assert.Equal(14, data[3]);
            }
        }

        [Fact]
        public void WriteByteWorksIfSegmentHasSpace_CopyTo()
        {
            var input = new byte[] { 11, 12, 13 };

            using (var bufferWriter = new MemoryBufferWriter())
            {
                bufferWriter.Write(input, 0, input.Length);
                bufferWriter.WriteByte(14);

                Assert.Equal(4, bufferWriter.Length);

                var data = new byte[bufferWriter.Length];

                bufferWriter.CopyTo(data);
                Assert.Equal(11, data[0]);
                Assert.Equal(12, data[1]);
                Assert.Equal(13, data[2]);
                Assert.Equal(14, data[3]);
            }
        }

        [Fact]
        public void ToArrayWithExactlyFullSegmentsWorks()
        {
            var inputSize = MinimumSegmentSize * 2;
            var input = Enumerable.Range(0, inputSize).Select(i => (byte)i).ToArray();

            using (var bufferWriter = new MemoryBufferWriter(MinimumSegmentSize))
            {
                bufferWriter.Write(input, 0, input.Length);
                Assert.Equal(input.Length, bufferWriter.Length);

                var data = bufferWriter.ToArray();
                Assert.Equal(input, data);
            }
        }

        [Fact]
        public void ToArrayWithExactlyFullSegmentsWorks_CopyTo()
        {
            var inputSize = MinimumSegmentSize * 2;
            var input = Enumerable.Range(0, inputSize).Select(i => (byte)i).ToArray();

            using (var bufferWriter = new MemoryBufferWriter(MinimumSegmentSize))
            {
                bufferWriter.Write(input, 0, input.Length);
                Assert.Equal(input.Length, bufferWriter.Length);

                var data = new byte[bufferWriter.Length];

                bufferWriter.CopyTo(data);
                Assert.Equal(input, data);
            }
        }

        [Fact]
        public void ToArrayWithSomeFullSegmentsWorks()
        {
            var inputSize = (MinimumSegmentSize * 2) + 1;
            var input = Enumerable.Range(0, inputSize).Select(i => (byte)i).ToArray();

            using (var bufferWriter = new MemoryBufferWriter(MinimumSegmentSize))
            {
                bufferWriter.Write(input, 0, input.Length);
                Assert.Equal(input.Length, bufferWriter.Length);

                var data = bufferWriter.ToArray();
                Assert.Equal(input, data);
            }
        }

        [Fact]
        public void ToArrayWithSomeFullSegmentsWorks_CopyTo()
        {
            var inputSize = (MinimumSegmentSize * 2) + 1;
            var input = Enumerable.Range(0, inputSize).Select(i => (byte)i).ToArray();

            using (var bufferWriter = new MemoryBufferWriter(MinimumSegmentSize))
            {
                bufferWriter.Write(input, 0, input.Length);
                Assert.Equal(input.Length, bufferWriter.Length);
                var data = new byte[bufferWriter.Length];

                bufferWriter.CopyTo(data);
                Assert.Equal(input, data);
            }
        }

        [Fact]
        public async Task CopyToAsyncWithExactlyFullSegmentsWorks()
        {
            var inputSize = MinimumSegmentSize * 2;
            var input = Enumerable.Range(0, inputSize).Select(i => (byte)i).ToArray();

            using (var bufferWriter = new MemoryBufferWriter(MinimumSegmentSize))
            {
                bufferWriter.Write(input, 0, input.Length);
                Assert.Equal(input.Length, bufferWriter.Length);

                var ms = new MemoryStream();
                await bufferWriter.CopyToAsync(ms);
                var data = ms.ToArray();
                Assert.Equal(input, data);
            }
        }

        [Fact]
        public async Task CopyToAsyncWithSomeFullSegmentsWorks()
        {
            // 2 segments + 1 extra byte
            var inputSize = (MinimumSegmentSize * 2) + 1;
            var input = Enumerable.Range(0, inputSize).Select(i => (byte)i).ToArray();

            using (var bufferWriter = new MemoryBufferWriter(MinimumSegmentSize))
            {
                bufferWriter.Write(input, 0, input.Length);
                Assert.Equal(input.Length, bufferWriter.Length);

                var ms = new MemoryStream();
                await bufferWriter.CopyToAsync(ms);
                var data = ms.ToArray();
                Assert.Equal(input, data);
            }
        }

        [Fact]
        public void CopyToWithExactlyFullSegmentsWorks()
        {
            var inputSize = MinimumSegmentSize * 2;
            var input = Enumerable.Range(0, inputSize).Select(i => (byte)i).ToArray();

            using (var bufferWriter = new MemoryBufferWriter(MinimumSegmentSize))
            {
                bufferWriter.Write(input, 0, input.Length);
                Assert.Equal(input.Length, bufferWriter.Length);

                using (var destination = new MemoryBufferWriter())
                {
                    bufferWriter.CopyTo(destination);
                    var data = destination.ToArray();
                    Assert.Equal(input, data);
                }
            }
        }


        [Fact]
        public void CopyToWithExactlyFullSegmentsWorks_CopyTo()
        {
            var inputSize = MinimumSegmentSize * 2;
            var input = Enumerable.Range(0, inputSize).Select(i => (byte)i).ToArray();

            using (var bufferWriter = new MemoryBufferWriter(MinimumSegmentSize))
            {
                bufferWriter.Write(input, 0, input.Length);
                Assert.Equal(input.Length, bufferWriter.Length);

                using (var destination = new MemoryBufferWriter())
                {
                    bufferWriter.CopyTo(destination);
                    var data = new byte[bufferWriter.Length];

                    bufferWriter.CopyTo(data);
                    Assert.Equal(input, data);

                    Array.Clear(data, 0, data.Length);

                    destination.CopyTo(data);
                    Assert.Equal(input, data);
                }
            }
        }

        [Fact]
        public void CopyToWithSomeFullSegmentsWorks()
        {
            var inputSize = (MinimumSegmentSize * 2) + 1;
            var input = Enumerable.Range(0, inputSize).Select(i => (byte)i).ToArray();

            using (var bufferWriter = new MemoryBufferWriter(MinimumSegmentSize))
            {
                bufferWriter.Write(input, 0, input.Length);
                Assert.Equal(input.Length, bufferWriter.Length);

                using (var destination = new MemoryBufferWriter())
                {
                    bufferWriter.CopyTo(destination);
                    var data = destination.ToArray();
                    Assert.Equal(input, data);
                }
            }
        }


        [Fact]
        public void CopyToWithSomeFullSegmentsWorks_CopyTo()
        {
            var inputSize = (MinimumSegmentSize * 2) + 1;
            var input = Enumerable.Range(0, inputSize).Select(i => (byte)i).ToArray();

            using (var bufferWriter = new MemoryBufferWriter(MinimumSegmentSize))
            {
                bufferWriter.Write(input, 0, input.Length);
                Assert.Equal(input.Length, bufferWriter.Length);

                using (var destination = new MemoryBufferWriter())
                {
                    bufferWriter.CopyTo(destination);
                    var data = new byte[bufferWriter.Length];
                    bufferWriter.CopyTo(data);

                    Assert.Equal(input, data);

                    Array.Clear(data, 0, data.Length);

                    destination.CopyTo(data);
                    Assert.Equal(input, data);
                }
            }
        }

#if NETCOREAPP
        [Fact]
        public void WriteSpanWorksAtNonZeroOffset()
        {
            using (var bufferWriter = new MemoryBufferWriter())
            {
                bufferWriter.WriteByte(1);
                bufferWriter.Write(new byte[] { 2, 3, 4 }.AsSpan());

                Assert.Equal(4, bufferWriter.Length);

                var data = bufferWriter.ToArray();
                Assert.Equal(4, data.Length);
                Assert.Equal(1, data[0]);
                Assert.Equal(2, data[1]);
                Assert.Equal(3, data[2]);
                Assert.Equal(4, data[3]);
            }
        }

        [Fact]
        public void WriteSpanWorksAtNonZeroOffset_CopyTo()
        {
            using (var bufferWriter = new MemoryBufferWriter())
            {
                bufferWriter.WriteByte(1);
                bufferWriter.Write(new byte[] { 2, 3, 4 }.AsSpan());

                Assert.Equal(4, bufferWriter.Length);

                var data = new byte[bufferWriter.Length];
                bufferWriter.CopyTo(data);
                Assert.Equal(1, data[0]);
                Assert.Equal(2, data[1]);
                Assert.Equal(3, data[2]);
                Assert.Equal(4, data[3]);
            }
        }
#endif

        [Fact]
        public void GetMemoryAllocatesNewSegmentWhenInsufficientSpaceInCurrentSegment()
        {
            // Have the buffer writer rent only the minimum size segments from the pool.
            using (var bufferWriter = new MemoryBufferWriter(MinimumSegmentSize))
            {
                var data = new byte[MinimumSegmentSize];
                new Random().NextBytes(data);

                // Write half the minimum segment size
                bufferWriter.Write(data.AsSpan(0, MinimumSegmentSize / 2));

                // Request a new buffer of MinimumSegmentSize
                var buffer = bufferWriter.GetMemory(MinimumSegmentSize);
                Assert.Equal(MinimumSegmentSize, buffer.Length);

                // Write to the buffer
                bufferWriter.Write(data);

                // Verify the data was all written correctly
                var expectedOutput = new byte[MinimumSegmentSize + (MinimumSegmentSize / 2)];
                data.AsSpan(0, MinimumSegmentSize / 2).CopyTo(expectedOutput.AsSpan(0, MinimumSegmentSize / 2));
                data.CopyTo(expectedOutput, MinimumSegmentSize / 2);
                Assert.Equal(expectedOutput, bufferWriter.ToArray());
            }
        }

        [Fact]
        public void ResetResetsTheMemoryBufferWriter()
        {
            var bufferWriter = new MemoryBufferWriter();
            bufferWriter.WriteByte(1);
            Assert.Equal(1, bufferWriter.Length);
            bufferWriter.Reset();
            Assert.Equal(0, bufferWriter.Length);
        }

        [Fact]
        public void DisposeResetsTheMemoryBufferWriter()
        {
            var bufferWriter = new MemoryBufferWriter();
            bufferWriter.WriteByte(1);
            Assert.Equal(1, bufferWriter.Length);
            bufferWriter.Dispose();
            Assert.Equal(0, bufferWriter.Length);
        }
    }
}
