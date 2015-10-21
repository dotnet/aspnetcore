using System;
using System.Linq;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class MemoryPoolIterator2Tests : IDisposable
    {
        private readonly MemoryPool2 _pool;

        public MemoryPoolIterator2Tests()
        {
            _pool = new MemoryPool2();
        }

        public void Dispose()
        {
            _pool.Dispose();
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
            var block = _pool.Lease(256);
            var chars = raw.ToCharArray().Select(c => (byte)c).ToArray();
            Buffer.BlockCopy(chars, 0, block.Array, block.Start, chars.Length);
            block.End += chars.Length;

            var begin = block.GetIterator();
            var searchFor = search.ToCharArray();

            int found = -1;
            if (searchFor.Length == 1)
            {
                found = begin.Seek(searchFor[0]);
            }
            else if (searchFor.Length == 2)
            {
                found = begin.Seek(searchFor[0], searchFor[1]);
            }
            else if (searchFor.Length == 3)
            {
                found = begin.Seek(searchFor[0], searchFor[1], searchFor[2]);
            }
            else
            {
                Assert.False(true, "Invalid test sample.");
            }

            Assert.Equal(expectResult, found);
            Assert.Equal(expectIndex, begin.Index - block.Start);
        }

        [Fact]
        public void Put()
        {
            var blocks = new MemoryPoolBlock2[4];
            for (var i = 0; i < 4; ++i)
            {
                blocks[i] = _pool.Lease(16);
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
            Assert.False(head.Put(0xFF));
        }
    }
}
