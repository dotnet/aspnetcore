using Microsoft.AspNet.Server.Kestrel.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class MemoryPoolBlock2Tests
    {
        [Fact]
        public void IndexOfAnyWorks()
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
                    var hit = iterator.IndexOf(ch);
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

                for (var fragment = 0; fragment != 256; fragment += 4)
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
    }
}
