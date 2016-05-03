using System;
using System.Linq;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using System.Numerics;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class MemoryPoolBlockTests
    {
        [Fact]
        public void SeekWorks()
        {
            using (var pool = new MemoryPool())
            {
                var block = pool.Lease();
                foreach (var ch in Enumerable.Range(0, 256).Select(x => (byte)x))
                {
                    block.Array[block.End++] = ch;
                }

                var vectorMaxValues = new Vector<byte>(byte.MaxValue);

                var iterator = block.GetIterator();
                foreach (var ch in Enumerable.Range(0, 256).Select(x => (byte)x))
                {
                    var vectorCh = new Vector<byte>(ch);

                    var hit = iterator;
                    hit.Seek(ref vectorCh);
                    Assert.Equal(ch, iterator.GetLength(hit));

                    hit = iterator;
                    hit.Seek(ref vectorCh, ref vectorMaxValues);
                    Assert.Equal(ch, iterator.GetLength(hit));

                    hit = iterator;
                    hit.Seek(ref vectorMaxValues, ref vectorCh);
                    Assert.Equal(ch, iterator.GetLength(hit));

                    hit = iterator;
                    hit.Seek(ref vectorCh, ref vectorMaxValues, ref vectorMaxValues);
                    Assert.Equal(ch, iterator.GetLength(hit));

                    hit = iterator;
                    hit.Seek(ref vectorMaxValues, ref vectorCh, ref vectorMaxValues);
                    Assert.Equal(ch, iterator.GetLength(hit));

                    hit = iterator;
                    hit.Seek(ref vectorCh, ref vectorMaxValues, ref vectorMaxValues);
                    Assert.Equal(ch, iterator.GetLength(hit));
                }

                pool.Return(block);
            }
        }

        [Fact]
        public void SeekWorksAcrossBlocks()
        {
            Console.WriteLine($"Vector.IsHardwareAccelerated == {Vector.IsHardwareAccelerated}");
            Console.WriteLine($"Vector<byte>.Count == {Vector<byte>.Count}");

            using (var pool = new MemoryPool())
            {
                var block1 = pool.Lease();
                var block2 = block1.Next = pool.Lease();
                var block3 = block2.Next = pool.Lease();

                foreach (var ch in Enumerable.Range(0, 34).Select(x => (byte)x))
                {
                    block1.Array[block1.End++] = ch;
                }
                foreach (var ch in Enumerable.Range(34, 25).Select(x => (byte)x))
                {
                    block2.Array[block2.End++] = ch;
                }
                foreach (var ch in Enumerable.Range(59, 197).Select(x => (byte)x))
                {
                    block3.Array[block3.End++] = ch;
                }

                var vectorMaxValues = new Vector<byte>(byte.MaxValue);

                var iterator = block1.GetIterator();
                foreach (var ch in Enumerable.Range(0, 256).Select(x => (byte)x))
                {
                    var vectorCh = new Vector<byte>(ch);

                    var hit = iterator;
                    hit.Seek(ref vectorCh);
                    Assert.Equal(ch, iterator.GetLength(hit));

                    hit = iterator;
                    hit.Seek(ref vectorCh, ref vectorMaxValues);
                    Assert.Equal(ch, iterator.GetLength(hit));

                    hit = iterator;
                    hit.Seek(ref vectorMaxValues, ref vectorCh);
                    Assert.Equal(ch, iterator.GetLength(hit));

                    hit = iterator;
                    hit.Seek(ref vectorCh, ref vectorMaxValues, ref vectorMaxValues);
                    Assert.Equal(ch, iterator.GetLength(hit));

                    hit = iterator;
                    hit.Seek(ref vectorMaxValues, ref vectorCh, ref vectorMaxValues);
                    Assert.Equal(ch, iterator.GetLength(hit));

                    hit = iterator;
                    hit.Seek(ref vectorMaxValues, ref vectorMaxValues, ref vectorCh);
                    Assert.Equal(ch, iterator.GetLength(hit));
                }

                pool.Return(block1);
                pool.Return(block2);
                pool.Return(block3);
            }
        }

        [Fact]
        public void GetLengthBetweenIteratorsWorks()
        {
            using (var pool = new MemoryPool())
            {
                var block = pool.Lease();
                block.End += 256;
                TestAllLengths(block, 256);
                pool.Return(block);
                block = null;

                for (var fragment = 0; fragment < 256; fragment += 4)
                {
                    var next = block;
                    block = pool.Lease();
                    block.Next = next;
                    block.End += 4;
                }

                TestAllLengths(block, 256);

                while(block != null)
                {
                    var next = block.Next;
                    pool.Return(block);
                    block = next;
                }
            }
        }

        private void TestAllLengths(MemoryPoolBlock block, int lengths)
        {
            for (var firstIndex = 0; firstIndex <= lengths; ++firstIndex)
            {
                for (var lastIndex = firstIndex; lastIndex <= lengths; ++lastIndex)
                {
                    var first = block.GetIterator().Add(firstIndex);
                    var last = block.GetIterator().Add(lastIndex);
                    Assert.Equal(firstIndex, block.GetIterator().GetLength(first));
                    Assert.Equal(lastIndex, block.GetIterator().GetLength(last));
                    Assert.Equal(lastIndex - firstIndex, first.GetLength(last));
                }
            }
        }

        [Fact]
        public void AddDoesNotAdvanceAtEndOfCurrentBlock()
        {
            using (var pool = new MemoryPool())
            {
                var block1 = pool.Lease();
                var block2 = block1.Next = pool.Lease();

                block1.End += 100;
                block2.End += 200;

                var iter0 = block1.GetIterator();
                var iter100 = iter0.Add(100);

                var iter200a = iter0.Add(200);
                var iter200b = iter100.Add(100);

                var iter300a = iter0.Add(300);
                var iter300b = iter100.Add(200);
                var iter300c = iter200a.Add(100);

                var iter300a2 = iter300a.Add(1);
                var iter300b2 = iter300b.Add(1);
                var iter300c2 = iter300c.Add(1);

                AssertIterator(iter0, block1, block1.Start);
                AssertIterator(iter100, block1, block1.End);
                AssertIterator(iter200a, block2, block2.Start+100);
                AssertIterator(iter200b, block2, block2.Start + 100);
                AssertIterator(iter300a, block2, block2.End);
                AssertIterator(iter300b, block2, block2.End);
                AssertIterator(iter300c, block2, block2.End);
                AssertIterator(iter300a2, block2, block2.End);
                AssertIterator(iter300b2, block2, block2.End);
                AssertIterator(iter300c2, block2, block2.End);

                pool.Return(block1);
                pool.Return(block2);
            }
        }

        [Fact]
        public void CopyToCorrectlyTraversesBlocks()
        {
            using (var pool = new MemoryPool())
            {
                var block1 = pool.Lease();
                var block2 = block1.Next = pool.Lease();

                for (int i = 0; i < 128; i++)
                {
                    block1.Array[block1.End++] = (byte)i;
                }
                for (int i = 128; i < 256; i++)
                {
                    block2.Array[block2.End++] = (byte)i;
                }

                var beginIterator = block1.GetIterator();
                
                var array = new byte[256];
                int actual;
                var endIterator = beginIterator.CopyTo(array, 0, 256, out actual);

                Assert.Equal(256, actual);

                for (int i = 0; i < 256; i++)
                {
                    Assert.Equal(i, array[i]);
                }

                endIterator.CopyTo(array, 0, 256, out actual);
                Assert.Equal(0, actual);

                pool.Return(block1);
                pool.Return(block2);
            }
        }

        [Fact]
        public void CopyFromCorrectlyTraversesBlocks()
        {
            using (var pool = new MemoryPool())
            {
                var block1 = pool.Lease();
                var start = block1.GetIterator();
                var end = start;
                var bufferSize = block1.Data.Count * 3;
                var buffer = new byte[bufferSize];

                for (int i = 0; i < bufferSize; i++)
                {
                    buffer[i] = (byte)(i % 73);
                }

                Assert.Null(block1.Next);

                end.CopyFrom(new ArraySegment<byte>(buffer));

                Assert.NotNull(block1.Next);

                for (int i = 0; i < bufferSize; i++)
                {
                    Assert.Equal(i % 73, start.Take());
                }

                Assert.Equal(-1, start.Take());
                Assert.Equal(start.Block, end.Block);
                Assert.Equal(start.Index, end.Index);

                var block = block1;
                while (block != null)
                {
                    var returnBlock = block;
                    block = block.Next;

                    pool.Return(returnBlock);
                }
            }
        }

        [Fact]
        public void IsEndCorrectlyTraversesBlocks()
        {
            using (var pool = new MemoryPool())
            {
                var block1 = pool.Lease();
                var block2 = block1.Next = pool.Lease();
                var block3 = block2.Next = pool.Lease();
                var block4 = block3.Next = pool.Lease();

                // There is no data in block2 or block4, so IsEnd should be true after 256 bytes are read.
                block1.End += 128;
                block3.End += 128;

                var iterStart = block1.GetIterator();
                var iterMid = iterStart.Add(128);
                var iterEnd = iterMid.Add(128);

                Assert.False(iterStart.IsEnd);
                Assert.False(iterMid.IsEnd);
                Assert.True(iterEnd.IsEnd);

                pool.Return(block1);
                pool.Return(block2);
                pool.Return(block3);
                pool.Return(block4);
            }
        }

        private void AssertIterator(MemoryPoolIterator iter, MemoryPoolBlock block, int index)
        {
            Assert.Same(block, iter.Block);
            Assert.Equal(index, iter.Index);
        }
    }
}
