// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Xunit;
using MemoryPool = Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure.MemoryPool;
using MemoryPoolBlock = Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure.MemoryPoolBlock;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class MemoryPoolIteratorTests : IDisposable
    {
        private readonly MemoryPool _pool;

        public MemoryPoolIteratorTests()
        {
            _pool = new MemoryPool();
        }

        public void Dispose()
        {
            _pool.Dispose();
        }

        [Fact]
        public void TestFindFirstEqualByte()
        {
            var bytes = Enumerable.Repeat<byte>(0xff, Vector<byte>.Count).ToArray();
            for (int i = 0; i < Vector<byte>.Count; i++)
            {
                Vector<byte> vector = new Vector<byte>(bytes);
                Assert.Equal(i, MemoryPoolIterator.LocateFirstFoundByte(vector));
                bytes[i] = 0;
            }

            for (int i = 0; i < Vector<byte>.Count; i++)
            {
                bytes[i] = 1;
                Vector<byte> vector = new Vector<byte>(bytes);
                Assert.Equal(i, MemoryPoolIterator.LocateFirstFoundByte(vector));
                bytes[i] = 0;
            }
        }

        [Theory]
        [InlineData("a", "a", 'a', 0)]
        [InlineData("ab", "a", 'a', 0)]
        [InlineData("aab", "a", 'a', 0)]
        [InlineData("acab", "a", 'a', 0)]
        [InlineData("acab", "c", 'c', 1)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "lo", 'l', 11)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "ol", 'l', 11)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "ll", 'l', 11)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "lmr", 'l', 11)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "rml", 'l', 11)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "mlr", 'l', 11)]
        [InlineData("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz", "lmr", 'l', 11)]
        [InlineData("aaaaaaaaaaalmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz", "lmr", 'l', 11)]
        [InlineData("aaaaaaaaaaacmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz", "lmr", 'm', 12)]
        [InlineData("aaaaaaaaaaarmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz", "lmr", 'r', 11)]
        [InlineData("/localhost:5000/PATH/%2FPATH2/ HTTP/1.1", " %?", '%', 21)]
        [InlineData("/localhost:5000/PATH/%2FPATH2/?key=value HTTP/1.1", " %?", '%', 21)]
        [InlineData("/localhost:5000/PATH/PATH2/?key=value HTTP/1.1", " %?", '?', 27)]
        [InlineData("/localhost:5000/PATH/PATH2/ HTTP/1.1", " %?", ' ', 27)]
        public void MemorySeek(string raw, string search, char expectResult, int expectIndex)
        {
            var block = _pool.Lease();
            var chars = raw.ToCharArray().Select(c => (byte)c).ToArray();
            Buffer.BlockCopy(chars, 0, block.Array, block.Start, chars.Length);
            block.End += chars.Length;

            var begin = block.GetIterator();
            var searchFor = search.ToCharArray();

            int found = -1;
            if (searchFor.Length == 1)
            {
                found = begin.Seek((byte)searchFor[0]);
            }
            else if (searchFor.Length == 2)
            {
                found = begin.Seek((byte)searchFor[0], (byte)searchFor[1]);
            }
            else if (searchFor.Length == 3)
            {
                found = begin.Seek((byte)searchFor[0], (byte)searchFor[1], (byte)searchFor[2]);
            }
            else
            {
                Assert.False(true, "Invalid test sample.");
            }

            Assert.Equal(expectResult, found);
            Assert.Equal(expectIndex, begin.Index - block.Start);

            _pool.Return(block);
        }

        [Fact]
        public void Put()
        {
            var blocks = new MemoryPoolBlock[4];
            for (var i = 0; i < 4; ++i)
            {
                blocks[i] = _pool.Lease();
                blocks[i].End += 16;

                for (var j = 0; j < blocks.Length; ++j)
                {
                    blocks[i].Array[blocks[i].Start + j] = 0x00;
                }

                if (i != 0)
                {
                    blocks[i - 1].Next = blocks[i];
                }
            }

            // put FF at first block's head
            var head = blocks[0].GetIterator();
            Assert.True(head.Put(0xFF));

            // data is put at correct position
            Assert.Equal(0xFF, blocks[0].Array[blocks[0].Start]);
            Assert.Equal(0x00, blocks[0].Array[blocks[0].Start + 1]);

            // iterator is moved to next byte after put
            Assert.Equal(1, head.Index - blocks[0].Start);

            for (var i = 0; i < 14; ++i)
            {
                // move itr to the end of the block 0
                head.Take();
            }

            // write to the end of block 0
            Assert.True(head.Put(0xFE));
            Assert.Equal(0xFE, blocks[0].Array[blocks[0].End - 1]);
            Assert.Equal(0x00, blocks[1].Array[blocks[1].Start]);

            // put data across the block link
            Assert.True(head.Put(0xFD));
            Assert.Equal(0xFD, blocks[1].Array[blocks[1].Start]);
            Assert.Equal(0x00, blocks[1].Array[blocks[1].Start + 1]);

            // paint every block
            head = blocks[0].GetIterator();
            for (var i = 0; i < 64; ++i)
            {
                Assert.True(head.Put((byte)i), $"Fail to put data at {i}.");
            }

            // Can't put anything by the end
            Assert.ThrowsAny<InvalidOperationException>(() => head.Put(0xFF));

            for (var i = 0; i < 4; ++i)
            {
                _pool.Return(blocks[i]);
            }
        }

        [Fact]
        public async Task PeekArraySegment()
        {
            using (var pipeFactory = new PipeFactory())
            {
                // Arrange
                var pipe = pipeFactory.Create();
                var buffer = pipe.Writer.Alloc();
                buffer.Append(ReadableBuffer.Create(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
                await buffer.FlushAsync();
                
                // Act
                var result = await pipe.Reader.PeekAsync();

                // Assert
                Assert.Equal(new byte[] {0, 1, 2, 3, 4, 5, 6, 7}, result);

                pipe.Writer.Complete();
                pipe.Reader.Complete();
            }
        }

        [Fact]
        public async Task PeekArraySegmentAtEndOfDataReturnsDefaultArraySegment()
        {
            using (var pipeFactory = new PipeFactory())
            {
                // Arrange
                var pipe = pipeFactory.Create();
                pipe.Writer.Complete();

                // Act
                var result = await pipe.Reader.PeekAsync();

                // Assert
                // Assert.Equals doesn't work since xunit tries to access the underlying array.
                Assert.True(default(ArraySegment<byte>).Equals(result));

                pipe.Reader.Complete();
            }
        }

        [Fact]
        public async Task PeekArraySegmentAtBlockBoundary()
        {
            using (var pipeFactory = new PipeFactory())
            {
                var pipe = pipeFactory.Create();
                var buffer = pipe.Writer.Alloc();
                buffer.Append(ReadableBuffer.Create(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
                buffer.Append(ReadableBuffer.Create(new byte[] { 8, 9, 10, 11, 12, 13, 14, 15 }));
                await buffer.FlushAsync();

                // Act
                var result = await pipe.Reader.PeekAsync();

                // Assert
                Assert.Equal(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }, result);

                // Act
                // Advance past the data in the first block
                var readResult = pipe.Reader.ReadAsync().GetAwaiter().GetResult();
                pipe.Reader.Advance(readResult.Buffer.Move(readResult.Buffer.Start, 8));
                result = await pipe.Reader.PeekAsync();

                // Assert
                Assert.Equal(new byte[] { 8, 9, 10, 11, 12, 13, 14, 15 }, result);

                pipe.Writer.Complete();
                pipe.Reader.Complete();
            }

        }

        [Fact]
        public void PeekLong()
        {
            // Arrange
            var block = _pool.Lease();
            var bytes = BitConverter.GetBytes(0x0102030405060708UL);
            Buffer.BlockCopy(bytes, 0, block.Array, block.Start, bytes.Length);
            block.End += bytes.Length;
            var scan = block.GetIterator();
            var originalIndex = scan.Index;

            // Assert
            ulong result;
            Assert.True(scan.TryPeekLong(out result));
            Assert.Equal(0x0102030405060708UL, result);
            Assert.Equal(originalIndex, scan.Index);

            _pool.Return(block);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        public void PeekLongNotEnoughBytes(int totalBytes)
        {
            // Arrange
            var block = _pool.Lease();
            var bytes = BitConverter.GetBytes(0x0102030405060708UL);
            var bytesLength = totalBytes;
            Buffer.BlockCopy(bytes, 0, block.Array, block.Start, bytesLength);
            block.End += bytesLength;
            var scan = block.GetIterator();
            var originalIndex = scan.Index;

            // Assert
            ulong result;
            Assert.False(scan.TryPeekLong(out result));
            Assert.Equal(originalIndex, scan.Index);
            _pool.Return(block);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        public void PeekLongNotEnoughBytesAtBlockBoundary(int firstBlockBytes)
        {
            // Arrange
            var expectedResult = 0x0102030405060708UL;
            var nextBlockBytes = 7 - firstBlockBytes;

            var block = _pool.Lease();
            block.End += firstBlockBytes;

            var nextBlock = _pool.Lease();
            nextBlock.End += nextBlockBytes;

            block.Next = nextBlock;

            var bytes = BitConverter.GetBytes(expectedResult);
            Buffer.BlockCopy(bytes, 0, block.Array, block.Start, firstBlockBytes);
            Buffer.BlockCopy(bytes, firstBlockBytes, nextBlock.Array, nextBlock.Start, nextBlockBytes);

            var scan = block.GetIterator();
            var originalIndex = scan.Index;

            // Assert
            ulong result;
            Assert.False(scan.TryPeekLong(out result));
            Assert.Equal(originalIndex, scan.Index);

            _pool.Return(block);
            _pool.Return(nextBlock);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void PeekLongAtBlockBoundary(int firstBlockBytes)
        {
            // Arrange
            var expectedResult = 0x0102030405060708UL;
            var nonZeroData = 0xFF00FFFF0000FFFFUL;
            var nextBlockBytes = 8 - firstBlockBytes;

            var block = _pool.Lease();
            block.Start += 8;
            block.End = block.Start + firstBlockBytes;

            var nextBlock = _pool.Lease();
            nextBlock.Start += 8;
            nextBlock.End = nextBlock.Start + nextBlockBytes;

            block.Next = nextBlock;

            var bytes = BitConverter.GetBytes(expectedResult);
            Buffer.BlockCopy(bytes, 0, block.Array, block.Start, firstBlockBytes);
            Buffer.BlockCopy(bytes, firstBlockBytes, nextBlock.Array, nextBlock.Start, nextBlockBytes);

            // Fill in surrounding bytes with non-zero data
            var nonZeroBytes = BitConverter.GetBytes(nonZeroData);
            Buffer.BlockCopy(nonZeroBytes, 0, block.Array, block.Start - 8, 8);
            Buffer.BlockCopy(nonZeroBytes, 0, block.Array, block.End, 8);
            Buffer.BlockCopy(nonZeroBytes, 0, nextBlock.Array, nextBlock.Start - 8, 8);
            Buffer.BlockCopy(nonZeroBytes, 0, nextBlock.Array, nextBlock.End, 8);

            var scan = block.GetIterator();
            var originalIndex = scan.Index;

            // Assert
            ulong result;
            Assert.True(scan.TryPeekLong(out result));
            Assert.Equal(expectedResult, result);
            Assert.Equal(originalIndex, scan.Index);

            _pool.Return(block);
            _pool.Return(nextBlock);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void PeekLongAtBlockBoundarayWithMostSignificatBitsSet(int firstBlockBytes)
        {
            // Arrange
            var expectedResult = 0xFF02030405060708UL;
            var nonZeroData = 0xFF00FFFF0000FFFFUL;
            var nextBlockBytes = 8 - firstBlockBytes;

            var block = _pool.Lease();
            block.Start += 8;
            block.End = block.Start + firstBlockBytes;

            var nextBlock = _pool.Lease();
            nextBlock.Start += 8;
            nextBlock.End = nextBlock.Start + nextBlockBytes;

            block.Next = nextBlock;

            var expectedBytes = BitConverter.GetBytes(expectedResult);
            Buffer.BlockCopy(expectedBytes, 0, block.Array, block.Start, firstBlockBytes);
            Buffer.BlockCopy(expectedBytes, firstBlockBytes, nextBlock.Array, nextBlock.Start, nextBlockBytes);

            // Fill in surrounding bytes with non-zero data
            var nonZeroBytes = BitConverter.GetBytes(nonZeroData);
            Buffer.BlockCopy(nonZeroBytes, 0, block.Array, block.Start - 8, 8);
            Buffer.BlockCopy(nonZeroBytes, 0, block.Array, block.End, 8);
            Buffer.BlockCopy(nonZeroBytes, 0, nextBlock.Array, nextBlock.Start - 8, 8);
            Buffer.BlockCopy(nonZeroBytes, 0, nextBlock.Array, nextBlock.End, 8);

            var scan = block.GetIterator();
            var originalIndex = scan.Index;

            // Assert
            ulong result;
            Assert.True(scan.TryPeekLong(out result));
            Assert.Equal(expectedResult, result);
            Assert.Equal(originalIndex, scan.Index);

            _pool.Return(block);
            _pool.Return(nextBlock);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        public void SkipAtBlockBoundary(int blockBytes)
        {
            // Arrange
            var nextBlockBytes = 10 - blockBytes;

            var block = _pool.Lease();
            block.End += blockBytes;

            var nextBlock = _pool.Lease();
            nextBlock.End += nextBlockBytes;

            block.Next = nextBlock;

            var bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Buffer.BlockCopy(bytes, 0, block.Array, block.Start, blockBytes);
            Buffer.BlockCopy(bytes, blockBytes, nextBlock.Array, nextBlock.Start, nextBlockBytes);

            var scan = block.GetIterator();
            var originalIndex = scan.Index;

            // Act
            scan.Skip(8);
            var result = scan.Take();

            // Assert
            Assert.Equal(0x08, result);
            Assert.NotEqual(originalIndex, scan.Index);

            _pool.Return(block);
            _pool.Return(nextBlock);
        }

        [Fact]
        public void SkipThrowsWhenSkippingMoreBytesThanAvailableInSingleBlock()
        {
            // Arrange
            var block = _pool.Lease();
            block.End += 5;

            var scan = block.GetIterator();

            // Act/Assert
            Assert.ThrowsAny<InvalidOperationException>(() => scan.Skip(8));

            _pool.Return(block);
        }

        [Fact]
        public void SkipThrowsWhenSkippingMoreBytesThanAvailableInMultipleBlocks()
        {
            // Arrange
            var firstBlock = _pool.Lease();
            firstBlock.End += 3;

            var middleBlock = _pool.Lease();
            middleBlock.End += 1;
            firstBlock.Next = middleBlock;

            var finalBlock = _pool.Lease();
            finalBlock.End += 2;
            middleBlock.Next = finalBlock;

            var scan = firstBlock.GetIterator();

            // Act/Assert
            Assert.ThrowsAny<InvalidOperationException>(() => scan.Skip(8));

            _pool.Return(firstBlock);
            _pool.Return(middleBlock);
            _pool.Return(finalBlock);
        }


        [Theory]
        [MemberData(nameof(SeekByteLimitData))]
        public void TestSeekByteLimitWithinSameBlock(string input, char seek, int limit, int expectedBytesScanned, int expectedReturnValue)
        {
            MemoryPoolBlock block = null;

            try
            {
                // Arrange

                block = _pool.Lease();
                var chars = input.ToString().ToCharArray().Select(c => (byte) c).ToArray();
                Buffer.BlockCopy(chars, 0, block.Array, block.Start, chars.Length);
                block.End += chars.Length;
                var scan = block.GetIterator();

                // Act
                int bytesScanned;
                var returnValue = scan.Seek((byte)seek, out bytesScanned, limit);

                // Assert
                Assert.Equal(expectedBytesScanned, bytesScanned);
                Assert.Equal(expectedReturnValue, returnValue);

                Assert.Same(block, scan.Block);
                var expectedEndIndex = expectedReturnValue != -1 ?
                    block.Start + input.IndexOf(seek) :
                    block.Start + expectedBytesScanned;
                Assert.Equal(expectedEndIndex, scan.Index);
            }
            finally
            {
                // Cleanup
                if (block != null) _pool.Return(block);
            }
        }

        [Theory]
        [MemberData(nameof(SeekByteLimitData))]
        public void TestSeekByteLimitAcrossBlocks(string input, char seek, int limit, int expectedBytesScanned, int expectedReturnValue)
        {
            MemoryPoolBlock block1 = null;
            MemoryPoolBlock block2 = null;
            MemoryPoolBlock emptyBlock = null;

            try
            {
                // Arrange
                var input1 = input.Substring(0, input.Length / 2);
                block1 = _pool.Lease();
                var chars1 = input1.ToCharArray().Select(c => (byte)c).ToArray();
                Buffer.BlockCopy(chars1, 0, block1.Array, block1.Start, chars1.Length);
                block1.End += chars1.Length;

                emptyBlock = _pool.Lease();
                block1.Next = emptyBlock;

                var input2 = input.Substring(input.Length / 2);
                block2 = _pool.Lease();
                var chars2 = input2.ToCharArray().Select(c => (byte)c).ToArray();
                Buffer.BlockCopy(chars2, 0, block2.Array, block2.Start, chars2.Length);
                block2.End += chars2.Length;
                emptyBlock.Next = block2;

                var scan = block1.GetIterator();

                // Act
                int bytesScanned;
                var returnValue = scan.Seek((byte)seek, out bytesScanned, limit);

                // Assert
                Assert.Equal(expectedBytesScanned, bytesScanned);
                Assert.Equal(expectedReturnValue, returnValue);

                var seekCharIndex = input.IndexOf(seek);
                var expectedEndBlock = limit <= input.Length / 2 ?
                    block1 :
                    (seekCharIndex != -1 && seekCharIndex < input.Length / 2 ? block1 : block2);
                Assert.Same(expectedEndBlock, scan.Block);
                var expectedEndIndex = expectedReturnValue != -1 ?
                    expectedEndBlock.Start + (expectedEndBlock == block1 ? input1.IndexOf(seek) : input2.IndexOf(seek)) :
                    expectedEndBlock.Start + (expectedEndBlock == block1 ? expectedBytesScanned : expectedBytesScanned - (input.Length / 2));
                Assert.Equal(expectedEndIndex, scan.Index);
            }
            finally
            {
                // Cleanup
                if (block1 != null) _pool.Return(block1);
                if (emptyBlock != null) _pool.Return(emptyBlock);
                if (block2 != null) _pool.Return(block2);
            }
        }

        [Theory]
        [MemberData(nameof(SeekIteratorLimitData))]
        public void TestSeekIteratorLimitWithinSameBlock(string input, char seek, char limitAt, int expectedReturnValue)
        {
            MemoryPoolBlock block = null;

            try
            {
                // Arrange
                var afterSeek = (byte)'B';

                block = _pool.Lease();
                var chars = input.ToCharArray().Select(c => (byte)c).ToArray();
                Buffer.BlockCopy(chars, 0, block.Array, block.Start, chars.Length);
                block.End += chars.Length;
                var scan1 = block.GetIterator();
                var scan2_1 = scan1;
                var scan2_2 = scan1;
                var scan3_1 = scan1;
                var scan3_2 = scan1;
                var scan3_3 = scan1;
                var end = scan1;

                // Act
                var endReturnValue = end.Seek((byte)limitAt);
                var returnValue1 = scan1.Seek((byte)seek, ref end);
                var returnValue2_1 = scan2_1.Seek((byte)seek, afterSeek, ref end);
                var returnValue2_2 = scan2_2.Seek(afterSeek, (byte)seek, ref end);
                var returnValue3_1 = scan3_1.Seek((byte)seek, afterSeek, afterSeek, ref end);
                var returnValue3_2 = scan3_2.Seek(afterSeek, (byte)seek, afterSeek, ref end);
                var returnValue3_3 = scan3_3.Seek(afterSeek, afterSeek, (byte)seek, ref end);

                // Assert
                Assert.Equal(input.Contains(limitAt) ? limitAt : -1, endReturnValue);
                Assert.Equal(expectedReturnValue, returnValue1);
                Assert.Equal(expectedReturnValue, returnValue2_1);
                Assert.Equal(expectedReturnValue, returnValue2_2);
                Assert.Equal(expectedReturnValue, returnValue3_1);
                Assert.Equal(expectedReturnValue, returnValue3_2);
                Assert.Equal(expectedReturnValue, returnValue3_3);

                Assert.Same(block, scan1.Block);
                Assert.Same(block, scan2_1.Block);
                Assert.Same(block, scan2_2.Block);
                Assert.Same(block, scan3_1.Block);
                Assert.Same(block, scan3_2.Block);
                Assert.Same(block, scan3_3.Block);

                var expectedEndIndex = expectedReturnValue != -1 ? block.Start + input.IndexOf(seek) : end.Index;
                Assert.Equal(expectedEndIndex, scan1.Index);
                Assert.Equal(expectedEndIndex, scan2_1.Index);
                Assert.Equal(expectedEndIndex, scan2_2.Index);
                Assert.Equal(expectedEndIndex, scan3_1.Index);
                Assert.Equal(expectedEndIndex, scan3_2.Index);
                Assert.Equal(expectedEndIndex, scan3_3.Index);
            }
            finally
            {
                // Cleanup
                if (block != null) _pool.Return(block);
            }
        }

        [Theory]
        [MemberData(nameof(SeekIteratorLimitData))]
        public void TestSeekIteratorLimitAcrossBlocks(string input, char seek, char limitAt, int expectedReturnValue)
        {
            MemoryPoolBlock block1 = null;
            MemoryPoolBlock block2 = null;
            MemoryPoolBlock emptyBlock = null;

            try
            {
                // Arrange
                var afterSeek = (byte)'B';

                var input1 = input.Substring(0, input.Length / 2);
                block1 = _pool.Lease();
                var chars1 = input1.ToCharArray().Select(c => (byte)c).ToArray();
                Buffer.BlockCopy(chars1, 0, block1.Array, block1.Start, chars1.Length);
                block1.End += chars1.Length;

                emptyBlock = _pool.Lease();
                block1.Next = emptyBlock;

                var input2 = input.Substring(input.Length / 2);
                block2 = _pool.Lease();
                var chars2 = input2.ToCharArray().Select(c => (byte)c).ToArray();
                Buffer.BlockCopy(chars2, 0, block2.Array, block2.Start, chars2.Length);
                block2.End += chars2.Length;
                emptyBlock.Next = block2;

                var scan1 = block1.GetIterator();
                var scan2_1 = scan1;
                var scan2_2 = scan1;
                var scan3_1 = scan1;
                var scan3_2 = scan1;
                var scan3_3 = scan1;
                var end = scan1;

                // Act
                var endReturnValue = end.Seek((byte)limitAt);
                var returnValue1 = scan1.Seek((byte)seek, ref end);
                var returnValue2_1 = scan2_1.Seek((byte)seek, afterSeek, ref end);
                var returnValue2_2 = scan2_2.Seek(afterSeek, (byte)seek, ref end);
                var returnValue3_1 = scan3_1.Seek((byte)seek, afterSeek, afterSeek, ref end);
                var returnValue3_2 = scan3_2.Seek(afterSeek, (byte)seek, afterSeek, ref end);
                var returnValue3_3 = scan3_3.Seek(afterSeek, afterSeek, (byte)seek, ref end);

                // Assert
                Assert.Equal(input.Contains(limitAt) ? limitAt : -1, endReturnValue);
                Assert.Equal(expectedReturnValue, returnValue1);
                Assert.Equal(expectedReturnValue, returnValue2_1);
                Assert.Equal(expectedReturnValue, returnValue2_2);
                Assert.Equal(expectedReturnValue, returnValue3_1);
                Assert.Equal(expectedReturnValue, returnValue3_2);
                Assert.Equal(expectedReturnValue, returnValue3_3);

                var seekCharIndex = input.IndexOf(seek);
                var limitAtIndex = input.IndexOf(limitAt);
                var expectedEndBlock = seekCharIndex != -1 && seekCharIndex < input.Length / 2 ?
                    block1 :
                    (limitAtIndex != -1 && limitAtIndex < input.Length / 2 ? block1 : block2);
                Assert.Same(expectedEndBlock, scan1.Block);
                Assert.Same(expectedEndBlock, scan2_1.Block);
                Assert.Same(expectedEndBlock, scan2_2.Block);
                Assert.Same(expectedEndBlock, scan3_1.Block);
                Assert.Same(expectedEndBlock, scan3_2.Block);
                Assert.Same(expectedEndBlock, scan3_3.Block);

                var expectedEndIndex = expectedReturnValue != -1 ?
                    expectedEndBlock.Start + (expectedEndBlock == block1 ? input1.IndexOf(seek) : input2.IndexOf(seek)) :
                    end.Index;
                Assert.Equal(expectedEndIndex, scan1.Index);
                Assert.Equal(expectedEndIndex, scan2_1.Index);
                Assert.Equal(expectedEndIndex, scan2_2.Index);
                Assert.Equal(expectedEndIndex, scan3_1.Index);
                Assert.Equal(expectedEndIndex, scan3_2.Index);
                Assert.Equal(expectedEndIndex, scan3_3.Index);
            }
            finally
            {
                // Cleanup
                if (block1 != null) _pool.Return(block1);
                if (emptyBlock != null) _pool.Return(emptyBlock);
                if (block2 != null) _pool.Return(block2);
            }
        }

        [Fact]
        public void EmptyIteratorBehaviourIsValid()
        {
            const byte byteCr = (byte)'\n';
            ulong longValue;
            var end = default(MemoryPoolIterator);

            Assert.False(default(MemoryPoolIterator).TryPeekLong(out longValue));
            Assert.Null(default(MemoryPoolIterator).GetAsciiString(ref end));
            Assert.Null(default(MemoryPoolIterator).GetUtf8String(ref end));
            // Assert.Equal doesn't work for default(ArraySegments)
            Assert.True(default(MemoryPoolIterator).GetArraySegment(end).Equals(default(ArraySegment<byte>)));
            Assert.True(default(MemoryPoolIterator).IsDefault);
            Assert.True(default(MemoryPoolIterator).IsEnd);
            Assert.Equal(default(MemoryPoolIterator).Take(), -1);
            Assert.Equal(default(MemoryPoolIterator).Peek(), -1);
            Assert.Equal(default(MemoryPoolIterator).Seek(byteCr), -1);
            Assert.Equal(default(MemoryPoolIterator).Seek(byteCr, ref end), -1);
            Assert.Equal(default(MemoryPoolIterator).Seek(byteCr, byteCr), -1);
            Assert.Equal(default(MemoryPoolIterator).Seek(byteCr, byteCr, byteCr), -1);

            default(MemoryPoolIterator).CopyFrom(default(ArraySegment<byte>));
            default(MemoryPoolIterator).CopyFromAscii("");
            Assert.ThrowsAny<InvalidOperationException>(() => default(MemoryPoolIterator).Put(byteCr));
            Assert.ThrowsAny<InvalidOperationException>(() => default(MemoryPoolIterator).GetLength(end));
            Assert.ThrowsAny<InvalidOperationException>(() => default(MemoryPoolIterator).Skip(1));
        }

        [Fact]
        public void TestGetArraySegment()
        {
            MemoryPoolBlock block0 = null;
            MemoryPoolBlock block1 = null;

            var byteRange = Enumerable.Range(1, 127).Select(x => (byte)x).ToArray();
            try
            {
                // Arrange
                block0 = _pool.Lease();
                block1 = _pool.Lease();

                block0.GetIterator().CopyFrom(byteRange);
                block1.GetIterator().CopyFrom(byteRange);

                block0.Next = block1;

                var begin = block0.GetIterator();
                var end0 = begin;
                var end1 = begin;

                end0.Skip(byteRange.Length);
                end1.Skip(byteRange.Length * 2);

                // Act
                var as0 = begin.GetArraySegment(end0);
                var as1 = begin.GetArraySegment(end1);

                // Assert
                Assert.Equal(as0.Count, byteRange.Length);
                Assert.Equal(as1.Count, byteRange.Length * 2);

                for (var i = 1; i < byteRange.Length; i++)
                {
                    var asb0 = as0.Array[i + as0.Offset];
                    var asb1 = as1.Array[i + as1.Offset];
                    var b = byteRange[i];

                    Assert.Equal(asb0, b);
                    Assert.Equal(asb1, b);
                }

                for (var i = 1 + byteRange.Length; i < byteRange.Length * 2; i++)
                {
                    var asb1 = as1.Array[i + as1.Offset];
                    var b = byteRange[i - byteRange.Length];

                    Assert.Equal(asb1, b);
                }

            }
            finally
            {
                if (block0 != null) _pool.Return(block0);
                if (block1 != null) _pool.Return(block1);
            }
        }

        [Fact]
        public void TestTake()
        {
            MemoryPoolBlock block0 = null;
            MemoryPoolBlock block1 = null;
            MemoryPoolBlock block2 = null;
            MemoryPoolBlock emptyBlock0 = null;
            MemoryPoolBlock emptyBlock1 = null;

            var byteRange = Enumerable.Range(1, 127).Select(x => (byte)x).ToArray();
            try
            {
                // Arrange
                block0 = _pool.Lease();
                block1 = _pool.Lease();
                block2 = _pool.Lease();
                emptyBlock0 = _pool.Lease();
                emptyBlock1 = _pool.Lease();

                block0.GetIterator().CopyFrom(byteRange);
                block1.GetIterator().CopyFrom(byteRange);
                block2.GetIterator().CopyFrom(byteRange);

                var begin = block0.GetIterator();

                // Single block
                for (var i = 0; i < byteRange.Length; i++)
                {
                    var t = begin.Take();
                    var b = byteRange[i];

                    Assert.Equal(t, b);
                }

                Assert.Equal(begin.Take(), -1);

                // Dual block
                block0.Next = block1;
                begin = block0.GetIterator();

                for (var block = 0; block < 2; block++)
                {
                    for (var i = 0; i < byteRange.Length; i++)
                    {
                        var t = begin.Take();
                        var b = byteRange[i];

                        Assert.Equal(t, b);
                    }
                }

                Assert.Equal(begin.Take(), -1);

                // Multi block
                block1.Next = emptyBlock0;
                emptyBlock0.Next = emptyBlock1;
                emptyBlock1.Next = block2;
                begin = block0.GetIterator();

                for (var block = 0; block < 3; block++)
                {
                    for (var i = 0; i < byteRange.Length; i++)
                    {
                        var t = begin.Take();
                        var b = byteRange[i];

                        Assert.Equal(t, b);
                    }
                }

                Assert.Equal(begin.Take(), -1);
            }
            finally
            {
                if (block0 != null) _pool.Return(block0);
                if (block1 != null) _pool.Return(block1);
                if (block2 != null) _pool.Return(block2);
                if (emptyBlock0 != null) _pool.Return(emptyBlock0);
                if (emptyBlock1 != null) _pool.Return(emptyBlock1);
            }
        }

        [Fact]
        public void TestTakeEmptyBlocks()
        {
            MemoryPoolBlock emptyBlock0 = null;
            MemoryPoolBlock emptyBlock1 = null;
            MemoryPoolBlock emptyBlock2 = null;
            try
            {
                // Arrange
                emptyBlock0 = _pool.Lease();
                emptyBlock1 = _pool.Lease();
                emptyBlock2 = _pool.Lease();

                var beginEmpty = emptyBlock0.GetIterator();

                // Assert

                // No blocks
                Assert.Equal(default(MemoryPoolIterator).Take(), -1);

                // Single empty block
                Assert.Equal(beginEmpty.Take(), -1);

                // Dual empty block
                emptyBlock0.Next = emptyBlock1;
                beginEmpty = emptyBlock0.GetIterator();
                Assert.Equal(beginEmpty.Take(), -1);

                // Multi empty block
                emptyBlock1.Next = emptyBlock2;
                beginEmpty = emptyBlock0.GetIterator();
                Assert.Equal(beginEmpty.Take(), -1);
            }
            finally
            {
                if (emptyBlock0 != null) _pool.Return(emptyBlock0);
                if (emptyBlock1 != null) _pool.Return(emptyBlock1);
                if (emptyBlock2 != null) _pool.Return(emptyBlock2);
            }
        }

        [Theory]
        [InlineData("a", "a", 1)]
        [InlineData("ab", "a...", 1)]
        [InlineData("abcde", "abcde", 5)]
        [InlineData("abcde", "abcd...", 4)]
        [InlineData("abcde", "abcde", 6)]
        public void TestGetAsciiStringEscaped(string input, string expected, int maxChars)
        {
            MemoryPoolBlock block = null;

            try
            {
                // Arrange
                var buffer = new Span<byte>(Encoding.ASCII.GetBytes(input));

                // Act
                var result = buffer.GetAsciiStringEscaped(maxChars);

                // Assert
                Assert.Equal(expected, result);
            }
            finally
            {
                if (block != null) _pool.Return(block);
            }
        }

        [Fact]
        public void CorrectContentLengthsOutput()
        {
            using (var pool = new MemoryPool())
            {
                var block = pool.Lease();
                try
                {
                    for (var i = 0u; i <= 9u; i++)
                    {
                        block.Reset();
                        var iter = new MemoryPoolIterator(block);
                        iter.CopyFromNumeric(i);

                        Assert.Equal(block.Array[block.Start], (byte)(i + '0'));
                        Assert.Equal(block.End, block.Start + 1);
                        Assert.Equal(iter.Index, block.End);
                    }
                    for (var i = 10u; i <= 99u; i++)
                    {
                        block.Reset();
                        var iter = new MemoryPoolIterator(block);
                        iter.CopyFromNumeric(i);

                        Assert.Equal(block.Array[block.Start], (byte)((i / 10) + '0'));
                        Assert.Equal(block.Array[block.Start + 1], (byte)((i % 10) + '0'));

                        Assert.Equal(block.End, block.Start + 2);
                        Assert.Equal(iter.Index, block.End);
                    }
                    for (var i = 100u; i <= 999u; i++)
                    {
                        block.Reset();
                        var iter = new MemoryPoolIterator(block);
                        iter.CopyFromNumeric(i);

                        Assert.Equal(block.Array[block.Start], (byte)((i / 100) + '0'));
                        Assert.Equal(block.Array[block.Start + 1], (byte)(((i % 100) / 10) + '0'));
                        Assert.Equal(block.Array[block.Start + 2], (byte)((i % 10) + '0'));

                        Assert.Equal(block.End, block.Start + 3);
                        Assert.Equal(iter.Index, block.End);
                    }
                    for (var i = 1000u; i <= 9999u; i++)
                    {
                        block.Reset();
                        var iter = new MemoryPoolIterator(block);
                        iter.CopyFromNumeric(i);

                        Assert.Equal(block.Array[block.Start], (byte)((i / 1000) + '0'));
                        Assert.Equal(block.Array[block.Start + 1], (byte)(((i % 1000) / 100) + '0'));
                        Assert.Equal(block.Array[block.Start + 2], (byte)(((i % 100) / 10) + '0'));
                        Assert.Equal(block.Array[block.Start + 3], (byte)((i % 10) + '0'));

                        Assert.Equal(block.End, block.Start + 4);
                        Assert.Equal(iter.Index, block.End);
                    }
                    {
                        block.Reset();
                        var iter = new MemoryPoolIterator(block);
                        iter.CopyFromNumeric(ulong.MaxValue);

                        var outputBytes = Encoding.ASCII.GetBytes(ulong.MaxValue.ToString("0"));

                        for (var i = 0; i < outputBytes.Length; i++)
                        {
                            Assert.Equal(block.Array[block.Start + i], outputBytes[i]);
                        }

                        Assert.Equal(block.End, block.Start + outputBytes.Length);
                        Assert.Equal(iter.Index, block.End);
                    }
                }
                finally
                {
                    pool.Return(block);
                }
            }
        }

        public static IEnumerable<object[]> SeekByteLimitData
        {
            get
            {
                var vectorSpan = Vector<byte>.Count;

                // string input, char seek, int limit, int expectedBytesScanned, int expectedReturnValue
                var data = new List<object[]>();

                // Non-vector inputs

                data.Add(new object[] { "hello, world", 'h', 12, 1, 'h' });
                data.Add(new object[] { "hello, world", ' ', 12, 7, ' ' });
                data.Add(new object[] { "hello, world", 'd', 12, 12, 'd' });
                data.Add(new object[] { "hello, world", '!', 12, 12, -1 });
                data.Add(new object[] { "hello, world", 'h', 13, 1, 'h' });
                data.Add(new object[] { "hello, world", ' ', 13, 7, ' ' });
                data.Add(new object[] { "hello, world", 'd', 13, 12, 'd' });
                data.Add(new object[] { "hello, world", '!', 13, 12, -1 });
                data.Add(new object[] { "hello, world", 'h', 5, 1, 'h' });
                data.Add(new object[] { "hello, world", 'o', 5, 5, 'o' });
                data.Add(new object[] { "hello, world", ',', 5, 5, -1 });
                data.Add(new object[] { "hello, world", 'd', 5, 5, -1 });
                data.Add(new object[] { "abba", 'a', 4, 1, 'a' });
                data.Add(new object[] { "abba", 'b', 4, 2, 'b' });

                // Vector inputs

                // Single vector, no seek char in input, expect failure
                data.Add(new object[] { new string('a', vectorSpan), 'b', vectorSpan, vectorSpan, -1 });
                // Two vectors, no seek char in input, expect failure
                data.Add(new object[] { new string('a', vectorSpan * 2), 'b', vectorSpan * 2, vectorSpan * 2, -1 });
                // Two vectors plus non vector length (thus hitting slow path too), no seek char in input, expect failure
                data.Add(new object[] { new string('a', vectorSpan * 2 + vectorSpan / 2), 'b', vectorSpan * 2 + vectorSpan / 2, vectorSpan * 2 + vectorSpan / 2, -1 });

                // For each input length from 1/2 to 3 1/2 vector spans in 1/2 vector span increments...
                for (var length = vectorSpan / 2; length <= vectorSpan * 3 + vectorSpan / 2; length += vectorSpan / 2)
                {
                    // ...place the seek char at vector and input boundaries...
                    for (var i = Math.Min(vectorSpan - 1, length - 1); i < length; i += ((i + 1) % vectorSpan == 0) ? 1 : Math.Min(i + (vectorSpan - 1), length - 1))
                    {
                        var input = new StringBuilder(new string('a', length));
                        input[i] = 'b';

                        // ...and check with a seek byte limit before, at, and past the seek char position...
                        for (var limitOffset = -1; limitOffset <= 1; limitOffset++)
                        {
                            var limit = (i + 1) + limitOffset;

                            if (limit >= i + 1)
                            {
                                // ...that Seek() succeeds when the seek char is within that limit...
                                data.Add(new object[] { input.ToString(), 'b', limit, i + 1, 'b' });
                            }
                            else
                            {
                                // ...and fails when it's not.
                                data.Add(new object[] { input.ToString(), 'b', limit, Math.Min(length, limit), -1 });
                            }
                        }
                    }
                }

                return data;
            }
        }

        public static IEnumerable<object[]> SeekIteratorLimitData
        {
            get
            {
                var vectorSpan = Vector<byte>.Count;

                // string input, char seek, char limitAt, int expectedReturnValue
                var data = new List<object[]>();

                // Non-vector inputs

                data.Add(new object[] { "hello, world", 'h', 'd', 'h' });
                data.Add(new object[] { "hello, world", ' ', 'd', ' ' });
                data.Add(new object[] { "hello, world", 'd', 'd', 'd' });
                data.Add(new object[] { "hello, world", '!', 'd', -1 });
                data.Add(new object[] { "hello, world", 'h', 'w', 'h' });
                data.Add(new object[] { "hello, world", 'o', 'w', 'o' });
                data.Add(new object[] { "hello, world", 'r', 'w', -1 });
                data.Add(new object[] { "hello, world", 'd', 'w', -1 });

                // Vector inputs

                // Single vector, no seek char in input, expect failure
                data.Add(new object[] { new string('a', vectorSpan), 'b', 'b', -1 });
                // Two vectors, no seek char in input, expect failure
                data.Add(new object[] { new string('a', vectorSpan * 2), 'b', 'b', -1 });
                // Two vectors plus non vector length (thus hitting slow path too), no seek char in input, expect failure
                data.Add(new object[] { new string('a', vectorSpan * 2 + vectorSpan / 2), 'b', 'b', -1 });

                // For each input length from 1/2 to 3 1/2 vector spans in 1/2 vector span increments...
                for (var length = vectorSpan / 2; length <= vectorSpan * 3 + vectorSpan / 2; length += vectorSpan / 2)
                {
                    // ...place the seek char at vector and input boundaries...
                    for (var i = Math.Min(vectorSpan - 1, length - 1); i < length; i += ((i + 1) % vectorSpan == 0) ? 1 : Math.Min(i + (vectorSpan - 1), length - 1))
                    {
                        var input = new StringBuilder(new string('a', length));
                        input[i] = 'b';

                        // ...along with sentinel characters to seek the limit iterator to...
                        input[i - 1] = 'A';
                        if (i < length - 1) input[i + 1] = 'B';

                        // ...and check that Seek() succeeds with a limit iterator at or past the seek char position...
                        data.Add(new object[] { input.ToString(), 'b', 'b', 'b' });
                        if (i < length - 1) data.Add(new object[] { input.ToString(), 'b', 'B', 'b' });

                        // ...and fails with a limit iterator before the seek char position.
                        data.Add(new object[] { input.ToString(), 'b', 'A', -1 });
                    }
                }

                return data;
            }
        }
    }
}