// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Server.Kestrel.Exceptions;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class AsciiDecodingTests
    {
        [Fact]
        private void FullAsciiRangeSupported()
        {
            var byteRange = Enumerable.Range(1, 127).Select(x => (byte)x).ToArray();
            using (var pool = new MemoryPool())
            {
                var mem = pool.Lease();
                mem.GetIterator().CopyFrom(byteRange);

                var begin = mem.GetIterator();
                var end = GetIterator(begin, byteRange.Length);

                var s = begin.GetAsciiString(end);

                Assert.Equal(s.Length, byteRange.Length);

                for (var i = 1; i < byteRange.Length; i++)
                {
                    var sb = (byte)s[i];
                    var b = byteRange[i];

                    Assert.Equal(sb, b);
                }

                pool.Return(mem);
            }
        }

        [Theory]
        [InlineData(0x00)]
        [InlineData(0x80)]
        private void ExceptionThrownForZeroOrNonAscii(byte b)
        {
            for (var length = 1; length < 16; length++)
            {
                for (var position = 0; position < length; position++)
                {
                    var byteRange = Enumerable.Range(1, length).Select(x => (byte)x).ToArray();
                    byteRange[position] = b;

                    using (var pool = new MemoryPool())
                    {
                        var mem = pool.Lease();
                        mem.GetIterator().CopyFrom(byteRange);

                        var begin = mem.GetIterator();
                        var end = GetIterator(begin, byteRange.Length);

                        Assert.Throws<BadHttpRequestException>(() => begin.GetAsciiString(end));
                    }
                }
            }
        }

        [Fact]
        private void MultiBlockProducesCorrectResults()
        {
            var byteRange = Enumerable.Range(0, 512 + 64).Select(x => (byte)((x & 0x7f) | 0x01)).ToArray();
            var expectedByteRange = byteRange
                                    .Concat(byteRange)
                                    .Concat(byteRange)
                                    .Concat(byteRange)
                                    .ToArray();

            using (var pool = new MemoryPool())
            {
                var mem0 = pool.Lease();
                var mem1 = pool.Lease();
                var mem2 = pool.Lease();
                var mem3 = pool.Lease();
                mem0.GetIterator().CopyFrom(byteRange);
                mem1.GetIterator().CopyFrom(byteRange);
                mem2.GetIterator().CopyFrom(byteRange);
                mem3.GetIterator().CopyFrom(byteRange);

                mem0.Next = mem1;
                mem1.Next = mem2;
                mem2.Next = mem3;

                var begin = mem0.GetIterator();
                var end = GetIterator(begin, expectedByteRange.Length);

                var s = begin.GetAsciiString(end);

                Assert.Equal(s.Length, expectedByteRange.Length);

                for (var i = 0; i < expectedByteRange.Length; i++)
                {
                    var sb = (byte)((s[i] & 0x7f) | 0x01);
                    var b = expectedByteRange[i];

                    Assert.Equal(sb, b);
                }

                pool.Return(mem0);
                pool.Return(mem1);
                pool.Return(mem2);
                pool.Return(mem3);
            }
        }

        [Fact]
        private void LargeAllocationProducesCorrectResults()
        {
            var byteRange = Enumerable.Range(0, 16384 + 64).Select(x => (byte)((x & 0x7f) | 0x01)).ToArray();
            var expectedByteRange = byteRange.Concat(byteRange).ToArray();
            using (var pool = new MemoryPool())
            {
                var mem0 = pool.Lease();
                var mem1 = pool.Lease();
                mem0.GetIterator().CopyFrom(byteRange);
                mem1.GetIterator().CopyFrom(byteRange);

                var lastBlock = mem0;
                while (lastBlock.Next != null)
                {
                    lastBlock = lastBlock.Next;
                }
                lastBlock.Next = mem1;

                var begin = mem0.GetIterator();
                var end = GetIterator(begin, expectedByteRange.Length);

                var s = begin.GetAsciiString(end);

                Assert.Equal(expectedByteRange.Length, s.Length);

                for (var i = 0; i < expectedByteRange.Length; i++)
                {
                    var sb = (byte)((s[i] & 0x7f) | 0x01);
                    var b = expectedByteRange[i];

                    Assert.Equal(sb, b);
                }

                var block = mem0;
                while (block != null)
                {
                    var returnBlock = block;
                    block = block.Next;
                    pool.Return(returnBlock);
                }

                pool.Return(mem0);
                pool.Return(mem1);
            }
        }

        private MemoryPoolIterator GetIterator(MemoryPoolIterator begin, int displacement)
        {
            var result = begin;
            for (int i = 0; i < displacement; ++i)
            {
                result.Take();
            }

            return result;
        }
    }
}
