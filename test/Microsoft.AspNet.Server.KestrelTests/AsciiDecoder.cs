// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class AsciiDecoderTests
    {
        [Fact]
        private void FullByteRangeSupported()
        {
            var byteRange = Enumerable.Range(0, 255).Select(x => (byte)x).ToArray();

            var mem = MemoryPoolBlock2.Create(new ArraySegment<byte>(byteRange), IntPtr.Zero, null, null);
            mem.End = byteRange.Length;

            var begin = mem.GetIterator();
            var end = GetIterator(begin, byteRange.Length);

            var s = begin.GetAsciiString(end);

            Assert.Equal(s.Length, byteRange.Length);

            for (var i = 0; i < byteRange.Length; i++)
            {
                var sb = (byte)s[i];
                var b = byteRange[i];

                Assert.Equal(sb, b);
            }
        }

        [Fact]
        private void MultiBlockProducesCorrectResults()
        {
            var byteRange = Enumerable.Range(0, 512 + 64).Select(x => (byte)x).ToArray();
            var expectedByteRange = byteRange
                                    .Concat(byteRange)
                                    .Concat(byteRange)
                                    .Concat(byteRange)
                                    .ToArray();

            var mem0 = MemoryPoolBlock2.Create(new ArraySegment<byte>(byteRange), IntPtr.Zero, null, null);
            var mem1 = MemoryPoolBlock2.Create(new ArraySegment<byte>(byteRange), IntPtr.Zero, null, null);
            var mem2 = MemoryPoolBlock2.Create(new ArraySegment<byte>(byteRange), IntPtr.Zero, null, null);
            var mem3 = MemoryPoolBlock2.Create(new ArraySegment<byte>(byteRange), IntPtr.Zero, null, null);
            mem0.End = byteRange.Length;
            mem1.End = byteRange.Length;
            mem2.End = byteRange.Length;
            mem3.End = byteRange.Length;

            mem0.Next = mem1;
            mem1.Next = mem2;
            mem2.Next = mem3;

            var begin = mem0.GetIterator();
            var end = GetIterator(begin, expectedByteRange.Length);

            var s = begin.GetAsciiString(end);

            Assert.Equal(s.Length, expectedByteRange.Length);

            for (var i = 0; i < expectedByteRange.Length; i++)
            {
                var sb = (byte)s[i];
                var b = expectedByteRange[i];

                Assert.Equal(sb, b);
            }
        }

        [Fact]
        private void HeapAllocationProducesCorrectResults()
        {
            var byteRange = Enumerable.Range(0, 16384 + 64).Select(x => (byte)x).ToArray();
            var expectedByteRange = byteRange.Concat(byteRange).ToArray();

            var mem0 = MemoryPoolBlock2.Create(new ArraySegment<byte>(byteRange), IntPtr.Zero, null, null);
            var mem1 = MemoryPoolBlock2.Create(new ArraySegment<byte>(byteRange), IntPtr.Zero, null, null);
            mem0.End = byteRange.Length;
            mem1.End = byteRange.Length;

            mem0.Next = mem1;

            var begin = mem0.GetIterator();
            var end = GetIterator(begin, expectedByteRange.Length);

            var s = begin.GetAsciiString(end);

            Assert.Equal(s.Length, expectedByteRange.Length);

            for (var i = 0; i < expectedByteRange.Length; i++)
            {
                var sb = (byte)s[i];
                var b = expectedByteRange[i];

                Assert.Equal(sb, b);
            }
        }

        private MemoryPoolIterator2 GetIterator(MemoryPoolIterator2 begin, int displacement)
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
