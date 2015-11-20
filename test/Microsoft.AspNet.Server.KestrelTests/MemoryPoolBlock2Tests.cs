using System;
using System.Linq;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class MemoryPoolBlock2Tests
    {
        [Fact]
        public void SeekWorks()
        {
            using (var pool = new MemoryPool2())
            {
                var block = pool.Lease(256);
                foreach (var ch in Enumerable.Range(0, 256).Select(x => (byte)x))
                {
                    block.Array[block.End++] = ch;
                }
                var iterator = block.GetIterator();
                foreach (var ch in Enumerable.Range(0, 256).Select(x => (char)x))
                {
                    var hit = iterator;
                    hit.Seek(ch);
                    Assert.Equal(ch, iterator.GetLength(hit));

                    hit = iterator;
                    hit.Seek(ch, byte.MaxValue);
                    Assert.Equal(ch, iterator.GetLength(hit));

                    hit = iterator;
                    hit.Seek(byte.MaxValue, ch);
                    Assert.Equal(ch, iterator.GetLength(hit));
                }
            }
        }

        [Fact]
        public void GetLengthBetweenIteratorsWorks()
        {
            using (var pool = new MemoryPool2())
            {
                var block = pool.Lease(256);
                block.End += 256;
                TestAllLengths(block, 256);
                pool.Return(block);
                block = null;

                for (var fragment = 0; fragment < 256; fragment += 4)
                {
                    var next = block;
                    block = pool.Lease(4);
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

        private void TestAllLengths(MemoryPoolBlock2 block, int lengths)
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
            using (var pool = new MemoryPool2())
            {
                var block1 = pool.Lease(256);
                var block2 = block1.Next = pool.Lease(256);

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
            }
        }

        [Fact]
        public void CopyToCorrectlyTraversesBlocks()
        {
            using (var pool = new MemoryPool2())
            {
                var block1 = pool.Lease(128);
                var block2 = block1.Next = pool.Lease(128);

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
            }
        }

        [Fact]
        public void CopyFromCorrectlyTraversesBlocks()
        {
            using (var pool = new MemoryPool2())
            {
                var block1 = pool.Lease(128);
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
            }
        }

        [Fact]
        public void IsEndCorrectlyTraversesBlocks()
        {
            using (var pool = new MemoryPool2())
            {
                var block1 = pool.Lease(128);
                var block2 = block1.Next = pool.Lease(128);
                var block3 = block2.Next = pool.Lease(128);
                var block4 = block3.Next = pool.Lease(128);

                // There is no data in block2 or block4, so IsEnd should be true after 256 bytes are read.
                block1.End += 128;
                block3.End += 128;

                var iterStart = block1.GetIterator();
                var iterMid = iterStart.Add(128);
                var iterEnd = iterMid.Add(128);

                Assert.False(iterStart.IsEnd);
                Assert.False(iterMid.IsEnd);
                Assert.True(iterEnd.IsEnd);
            }
        }

        private void AssertIterator(MemoryPoolIterator2 iter, MemoryPoolBlock2 block, int index)
        {
            Assert.Same(block, iter.Block);
            Assert.Equal(index, iter.Index);
        }
    }
}
