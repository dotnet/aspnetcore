using System;
using System.Linq;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using System.Numerics;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
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

        [Fact]
        public void FindFirstByte()
        {
            var bytes = new byte[] {
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
            for (int i = 0; i < Vector<byte>.Count; i++)
            {
                Vector<byte> vector = new Vector<byte>(bytes);
                Assert.Equal(i, MemoryPoolIterator2.FindFirstEqualByte(ref vector));
                bytes[i] = 0;
            }
        }

        [Fact]
        public void _FindFirstByte()
        {
            var bytes = new byte[] {
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
            for (int i = 0; i < Vector<byte>.Count; i++)
            {
                Vector<byte> vector = new Vector<byte>(bytes);
                Assert.Equal(i, MemoryPoolIterator2.FindFirstEqualByteSlow(ref vector));
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
            var block = _pool.Lease(256);
            var chars = raw.ToCharArray().Select(c => (byte)c).ToArray();
            Buffer.BlockCopy(chars, 0, block.Array, block.Start, chars.Length);
            block.End += chars.Length;

            var begin = block.GetIterator();
            var searchFor = search.ToCharArray();

            int found = -1;
            if (searchFor.Length == 1)
            {
                var search0 = new Vector<byte>((byte)searchFor[0]);
                found = begin.Seek(ref search0);
            }
            else if (searchFor.Length == 2)
            {
                var search0 = new Vector<byte>((byte)searchFor[0]);
                var search1 = new Vector<byte>((byte)searchFor[1]);
                found = begin.Seek(ref search0, ref search1);
            }
            else if (searchFor.Length == 3)
            {
                var search0 = new Vector<byte>((byte)searchFor[0]);
                var search1 = new Vector<byte>((byte)searchFor[1]);
                var search2 = new Vector<byte>((byte)searchFor[2]);
                found = begin.Seek(ref search0, ref search1, ref search2);
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

        [Fact]
        public void PeekLong()
        {
            // Arrange
            var block = _pool.Lease();
            var bytes = BitConverter.GetBytes(0x0102030405060708);
            Buffer.BlockCopy(bytes, 0, block.Array, block.Start, bytes.Length);
            block.End += bytes.Length;
            var scan = block.GetIterator();
            var originalIndex = scan.Index;

            // Act
            var result = scan.PeekLong();

            // Assert
            Assert.Equal(0x0102030405060708, result);
            Assert.Equal(originalIndex, scan.Index);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        public void PeekLongAtBlockBoundary(int blockBytes)
        {
            // Arrange
            var nextBlockBytes = 8 - blockBytes;

            var block = _pool.Lease();
            block.End += blockBytes;

            var nextBlock = _pool.Lease();
            nextBlock.End += nextBlockBytes;

            block.Next = nextBlock;

            var bytes = BitConverter.GetBytes(0x0102030405060708);
            Buffer.BlockCopy(bytes, 0, block.Array, block.Start, blockBytes);
            Buffer.BlockCopy(bytes, blockBytes, nextBlock.Array, nextBlock.Start, nextBlockBytes);

            var scan = block.GetIterator();
            var originalIndex = scan.Index;

            // Act
            var result = scan.PeekLong();

            // Assert
            Assert.Equal(0x0102030405060708, result);
            Assert.Equal(originalIndex, scan.Index);
        }

        [Theory]
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
        }

        [Theory]
        [InlineData("CONNECT / HTTP/1.1", ' ', true, MemoryPoolIterator2Extensions.HttpConnectMethod)]
        [InlineData("DELETE / HTTP/1.1", ' ', true, MemoryPoolIterator2Extensions.HttpDeleteMethod)]
        [InlineData("GET / HTTP/1.1", ' ', true, MemoryPoolIterator2Extensions.HttpGetMethod)]
        [InlineData("HEAD / HTTP/1.1", ' ', true, MemoryPoolIterator2Extensions.HttpHeadMethod)]
        [InlineData("PATCH / HTTP/1.1", ' ', true, MemoryPoolIterator2Extensions.HttpPatchMethod)]
        [InlineData("POST / HTTP/1.1", ' ', true, MemoryPoolIterator2Extensions.HttpPostMethod)]
        [InlineData("PUT / HTTP/1.1", ' ', true, MemoryPoolIterator2Extensions.HttpPutMethod)]
        [InlineData("OPTIONS / HTTP/1.1", ' ', true, MemoryPoolIterator2Extensions.HttpOptionsMethod)]
        [InlineData("TRACE / HTTP/1.1", ' ', true, MemoryPoolIterator2Extensions.HttpTraceMethod)]
        [InlineData("GET/ HTTP/1.1", ' ', false, null)]
        [InlineData("get / HTTP/1.1", ' ', false, null)]
        [InlineData("GOT / HTTP/1.1", ' ', false, null)]
        [InlineData("ABC / HTTP/1.1", ' ', false, null)]
        [InlineData("PO / HTTP/1.1", ' ', false, null)]
        [InlineData("PO ST / HTTP/1.1", ' ', false, null)]
        [InlineData("short ", ' ', false, null)]
        public void GetsKnownMethod(string input, char endChar, bool expectedResult, string expectedKnownString)
        {
            // Arrange
            var block = _pool.Lease();
            var chars = input.ToCharArray().Select(c => (byte)c).ToArray();
            Buffer.BlockCopy(chars, 0, block.Array, block.Start, chars.Length);
            block.End += chars.Length;
            var scan = block.GetIterator();
            var begin = scan;
            string knownString;

            // Act
            var result = begin.GetKnownMethod(ref scan, out knownString);

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedKnownString, knownString);
        }

        [Theory]
        [InlineData("HTTP/1.0\r", '\r', true, MemoryPoolIterator2Extensions.Http10Version)]
        [InlineData("HTTP/1.1\r", '\r', true, MemoryPoolIterator2Extensions.Http11Version)]
        [InlineData("HTTP/3.0\r", '\r', false, null)]
        [InlineData("http/1.0\r", '\r', false, null)]
        [InlineData("http/1.1\r", '\r', false, null)]
        [InlineData("short ", ' ', false, null)]
        public void GetsKnownVersion(string input, char endChar, bool expectedResult, string expectedKnownString)
        {
            // Arrange
            var block = _pool.Lease();
            var chars = input.ToCharArray().Select(c => (byte)c).ToArray();
            Buffer.BlockCopy(chars, 0, block.Array, block.Start, chars.Length);
            block.End += chars.Length;
            var scan = block.GetIterator();
            var begin = scan;
            string knownString;

            // Act
            var result = begin.GetKnownVersion(ref scan, out knownString);
            // Assert
            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedKnownString, knownString);
        }
    }
}